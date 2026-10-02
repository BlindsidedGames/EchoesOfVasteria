#!/usr/bin/env python3
"""Run Echoes' Unity tests in a disposable, offline macOS sandbox."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import signal
import subprocess
import sys
import tempfile
import time
import xml.etree.ElementTree as ET


def write_json(path, value):
    path.write_text(json.dumps(value, indent=2) + "\n")


def summarize(path, process_exit=0):
    """NUnit leaf cases own counts; suite/setup failures also fail the run."""
    report = {"status": "failed", "process_exit": process_exit,
              "passed": 0, "failed": 0, "skipped": 0, "failures": []}
    try:
        root = ET.parse(path).getroot()
        cases = list(root.iter("test-case"))
        if root.tag != "test-run" or not cases:
            raise ValueError("No NUnit test cases were discovered; check assembly/filter routing.")
        for case in cases:
            result = case.get("result", "Unknown")
            if result == "Passed":
                report["passed"] += 1
            elif result in ("Skipped", "Inconclusive", "Ignored"):
                report["skipped"] += 1
            else:
                report["failed"] += 1
        for node in root.iter():
            failure = node.find("failure")
            if failure is not None:
                report["failures"].append({"name": node.get("fullname", node.get("name", root.tag)),
                                           "message": failure.findtext("message", ""),
                                           "stack": failure.findtext("stack-trace", "")})
        if (process_exit == 0 and root.get("result") == "Passed"
                and report["failed"] == 0 and report["passed"] > 0
                and not report["failures"]):
            report["status"] = "passed"
    except (OSError, ET.ParseError, ValueError) as error:
        report["error"] = str(error)
    return report


def snapshot(source, destination):
    """Only Unity inputs; no Library, saves, user settings or Git mutations."""
    records = {}
    for name in ("Assets", "Packages", "ProjectSettings"):
        base = source / name
        if base.is_symlink():
            raise ValueError(f"Refusing linked Unity input directory: {base}")
        if not base.is_dir():
            raise ValueError(f"Missing Unity input directory: {base}")
        for path in sorted(base.rglob("*")):
            if path.is_symlink():
                raise ValueError(f"Refusing linked input outside snapshot ownership: {path}")
            target = destination / path.relative_to(source)
            if path.is_dir():
                target.mkdir(parents=True, exist_ok=True)
            elif path.is_file():
                target.parent.mkdir(parents=True, exist_ok=True)
                before = path.stat()
                digest = hashlib.sha256()
                with path.open("rb") as reader, target.open("wb") as writer:
                    for chunk in iter(lambda: reader.read(1024 * 1024), b""):
                        digest.update(chunk)
                        writer.write(chunk)
                shutil.copystat(path, target)
                records[str(path.relative_to(source))] = {
                    "sha256": digest.hexdigest(), "size": before.st_size,
                    "mtime_ns": before.st_mtime_ns}
    # Detect concurrent edits during copy, including added and removed inputs.
    current = {str(p.relative_to(source)): p.stat() for name in
               ("Assets", "Packages", "ProjectSettings")
               for p in (source / name).rglob("*") if p.is_file()}
    if current.keys() != records.keys() or any(
            current[p].st_size != record["size"] or
            current[p].st_mtime_ns != record["mtime_ns"] for p, record in records.items()):
        raise ValueError("Unity inputs changed while copying. Retry after the author finishes editing.")
    return records


def sandbox_profile(run_root):
    # HOME alone is not a macOS NSHomeDirectory/PlayerPrefs isolation boundary.
    # Canonical paths handle /tmp -> /private/tmp on macOS.
    quoted = json.dumps(str(run_root.resolve()))
    home = Path.home().resolve()
    native_temp = Path(os.environ.get("TMPDIR", tempfile.gettempdir())).resolve()
    caches = [home / "Library/Application Support/Unity/CoreBusinessMetrics.db",
              home / "Library/Caches/com.unity3d.UnityEditor/CurlRequestCache.db"]
    # Native Unity caches ignore HOME. Permit only these cache DBs and sidecars,
    # never editor/game preference files or player save directories.
    allowed = [f"(subpath {quoted})", '(literal "/dev/null")',
               '(literal "/private/tmp/unitypreflock")',
               '(regex "^/private/tmp/Unity-ShaderCompilerIPC-[0-9]+-[0-9]+[.]lock$")',
               '(require-all (subpath "/private/tmp") (vnode-type SOCKET))',
               '(subpath "/private/tmp/.dotnet/shm")',
               '(subpath "/private/tmp/.dotnet/lockfiles")']
    allowed.append(f"(subpath {json.dumps(str(home / 'Library/Caches/com.unity3d.UnityEditor/GiCache'))})")
    for cache in caches:
        allowed.extend(f"(literal {json.dumps(str(cache) + suffix)})"
                       for suffix in ("", "-wal", "-shm", "-journal"))
    # Cocoa writes atomic replacement files here even when TMPDIR is overridden.
    temp_pattern = "^" + re.escape(str(native_temp / "TemporaryItems")) + "/NSIRD_Unity_[^/]+(/.*)?$"
    allowed.append(f"(regex {json.dumps(temp_pattern)})")
    # macOS's sandbox regex dialect does not implement {43} quantifiers.
    roslyn_pipe = "/(CoreFxPipe_)?" + "[A-Za-z0-9_+]" * 43 + "$"
    # Unix sockets are required for Unity's existing licensing client and UPM.
    # Unity shader compilation needs loopback IPC. External IP stays denied.
    return ("(version 1)\n(allow default)\n(deny network*)\n"
            "(allow network* (local unix-socket) (remote unix-socket))\n"
            '(allow network-bind (local ip "localhost:*"))\n'
            '(allow network-inbound (local ip "localhost:*"))\n'
            '(allow network-outbound (remote ip "localhost:*"))\n'
            # Roslyn's shared server would inherit another snapshot's sandbox.
            # Failed server connection uses Roslyn's local compiler fallback.
            '(deny network-outbound (remote unix-socket '
            f'(path-regex {json.dumps(roslyn_pipe)})))\n'
            "(deny file-write*)\n(allow file-write* " + " ".join(allowed) + ")\n")


def isolate_identity(project, run_root):
    """Change only the disposable project; cfprefsd must never target real prefs."""
    path = project / "ProjectSettings/ProjectSettings.asset"
    text = path.read_text()
    identity = "EOVTests_" + hashlib.sha256(str(run_root).encode()).hexdigest()[:16]
    for key, value in (("companyName", "EchoesTests"), ("productName", identity)):
        text, count = re.subn(rf"^  {key}:.*$", f"  {key}: {value}", text, flags=re.MULTILINE)
        if count != 1:
            raise ValueError(f"Cannot isolate {key}; expected one PlayerSettings entry")
    path.write_text(text)
    return {"companyName": "EchoesTests", "productName": identity}


def embed_cached_packages(source, project):
    """Copy exact locked registry packages as Unity-supported embedded packages.

    Merely copying Library/PackageCache still asks the registry for metadata.
    Embedded copies avoid that request without granting the game IP access.
    """
    lock_path = project / "Packages/packages-lock.json"
    if not lock_path.is_file():
        return {}
    locked = json.loads(lock_path.read_text())["dependencies"]
    required = {name: info["version"] for name, info in locked.items() if info["source"] == "registry"}
    found = {}
    records = {}
    cache = source / "Library/PackageCache"
    if cache.is_symlink():
        raise ValueError("Refusing linked package cache root")
    for directory in sorted(cache.iterdir()) if cache.is_dir() else ():
        if any(p.is_symlink() for p in directory.rglob("*")) or directory.is_symlink():
            raise ValueError(f"Linked package cache is not disposable: {directory}")
        manifest = directory / "package.json"
        if not manifest.is_file():
            continue
        info = json.loads(manifest.read_text())
        name = info["name"]
        if name not in required:
            continue
        if not re.fullmatch(r"[a-z0-9][a-z0-9._-]+", name):
            raise ValueError(f"Invalid package name: {name}")
        if name in found or info["version"] != required[name]:
            raise ValueError(f"Package cache is ambiguous or differs from lock: {name}")
        target = project / "Packages" / name
        if target.exists():
            raise ValueError(f"Registry cache conflicts with embedded package: {name}")
        before = {str(p.relative_to(directory)): (p.stat().st_size, p.stat().st_mtime_ns)
                  for p in directory.rglob("*") if p.is_file()}
        shutil.copytree(directory, target)
        after = {str(p.relative_to(directory)): (p.stat().st_size, p.stat().st_mtime_ns)
                 for p in directory.rglob("*") if p.is_file()}
        if before != after:
            raise ValueError(f"Package changed while copying: {name}")
        hashes = {str(p.relative_to(target)): hashlib.sha256(p.read_bytes()).hexdigest()
                  for p in target.rglob("*") if p.is_file()}
        records[name] = {"version": info["version"], "source": str(directory), "files": hashes}
        found[name] = info["version"]
    missing = required.keys() - found.keys()
    if missing:
        raise ValueError("Resolve packages in the authored project first; missing locked caches: "
                         + ", ".join(sorted(missing)))
    return records


def link_offline_license(home, disposable_home):
    """Let a replacement licensing client read installed offline entitlements.

    The sandbox still denies writes to the original license directory.
    License bytes are neither copied into artifacts nor logged.
    """
    source = home / "Library/Unity/licenses"
    if source.is_dir():
        target = disposable_home / "Library/Unity/licenses"
        target.parent.mkdir(parents=True, exist_ok=True)
        target.symlink_to(source.resolve(), target_is_directory=True)


def create_native_fixture_paths(home, run_root, identity, leases):
    """Bridge macOS native paths to scratch, using a unique test-only namespace."""
    for prefix in ("unity", "com"):
        pref = home / "Library/Preferences" / f"{prefix}.EchoesTests.{identity}.plist"
        if pref.exists() or pref.is_symlink():
            raise ValueError(f"Refusing pre-existing test preferences: {pref}")
        leases["prefs"].append(pref)
    for native, folder in (("Application Support", "saves"), ("Caches", "cache")):
        parent = home / "Library" / native / "EchoesTests"
        path = parent / identity
        if path.exists() or path.is_symlink():
            raise ValueError(f"Refusing pre-existing native test namespace: {path}")
        if not parent.exists():
            parent.mkdir(parents=True)
            leases["parents"].append(parent)
        if parent.is_symlink():
            raise ValueError(f"Refusing linked native fixture parent: {parent}")
        target = run_root / "native" / folder
        target.mkdir(parents=True)
        path.symlink_to(target, target_is_directory=True)
        leases["links"].append((path, target))


def cleanup_native_fixture_paths(leases):
    for path in leases["prefs"]:
        if path.is_file() or path.is_symlink():
            path.unlink()
    for path, target in reversed(leases["links"]):
        if path.is_symlink() and path.resolve() == target.resolve():
            path.unlink()
        elif path.exists():
            raise ValueError(f"Native fixture link changed; preserved for inspection: {path}")
    for parent in reversed(leases["parents"]):
        if parent.exists() and not any(parent.iterdir()):
            parent.rmdir()


def run_process(command, env, timeout):
    # Kill the whole Unity/import worker process group on timeout or interruption.
    with subprocess.Popen(command, env=env, start_new_session=True) as process:
        try:
            return process.wait(timeout=timeout)
        except (subprocess.TimeoutExpired, KeyboardInterrupt):
            os.killpg(process.pid, signal.SIGTERM)
            try:
                process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                os.killpg(process.pid, signal.SIGKILL)
                process.wait()
            raise


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--platform", choices=("EditMode", "PlayMode", "both"), default="EditMode")
    parser.add_argument("--filter", default="", help="Unity testFilter expression, e.g. ResourceDropTests")
    parser.add_argument("--unity", type=Path, help="Override exact-version Unity executable")
    parser.add_argument("--output", type=Path, help="New empty run directory outside the source project")
    parser.add_argument("--prepare-only", action="store_true", help="Snapshot and record command; do not start Unity")
    parser.add_argument("--timeout", type=int, default=1800, help="Seconds per suite, including first import")
    parser.add_argument("--summarize", type=Path, help="Summarize existing NUnit XML without starting Unity")
    args = parser.parse_args(argv)
    if args.summarize:
        report = summarize(args.summarize)
        print(json.dumps(report, indent=2))
        return 0 if report["status"] == "passed" else 1
    source = args.project.resolve()
    if args.timeout <= 0:
        parser.error("--timeout must be positive")
    version_file = source / "ProjectSettings/ProjectVersion.txt"
    match = re.search(r"^m_EditorVersion: (\S+)$", version_file.read_text(), re.MULTILINE)
    if not match:
        parser.error(f"Cannot find exact Unity version in {version_file}")
    version = match.group(1)
    unity = args.unity or Path(f"/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/MacOS/Unity")
    unity = unity.resolve()
    if not args.prepare_only and (sys.platform != "darwin" or not Path("/usr/bin/sandbox-exec").is_file()):
        parser.error("Execution requires macOS sandbox-exec; refusing an unisolated fallback")
    if not args.prepare_only and not unity.is_file():
        parser.error(f"Unity executable missing: {unity}")
    output = args.output.resolve() if args.output else Path(tempfile.mkdtemp(prefix="eov-tests-")).resolve()
    if output == source or source in output.parents or output in source.parents:
        parser.error("Run directory must be outside and not contain the source project")
    if args.output:
        if output.exists() and any(output.iterdir()):
            parser.error("--output must be absent or empty; old results cannot prove this run")
        output.mkdir(parents=True, exist_ok=True)
    result = {"status": "failed", "source": str(source), "run_root": str(output),
              "unity_version": version, "filter": args.filter, "suites": []}
    print(f"Run evidence: {output}", flush=True)
    leases = {"links": [], "parents": [], "prefs": []}
    try:
        project = output / "project"
        project.mkdir()
        write_json(output / "inputs.json", snapshot(source, project))
        result["disposable_identity"] = isolate_identity(project, output)
        for folder in ("home", "tmp", "cache"):
            (output / folder).mkdir()
        link_offline_license(Path.home(), output / "home")
        profile = output / "sandbox.sb"
        profile.write_text(sandbox_profile(output))
        env = os.environ.copy()
        for key in list(env):
            if key.lower() in ("http_proxy", "https_proxy", "all_proxy"):
                del env[key]
        env.update(HOME=str(output / "home"), TMPDIR=str(output / "tmp") + "/",
                   TMP=str(output / "tmp"), TEMP=str(output / "tmp"),
                   UPM_CACHE_ROOT=str(output / "cache"))
        write_json(output / "package-inputs.json", embed_cached_packages(source, project))
        if not args.prepare_only:
            create_native_fixture_paths(Path.home(), output,
                                        result["disposable_identity"]["productName"], leases)
            result["native_fixture_links"] = [str(path) for path, _ in leases["links"]]
        platforms = ("EditMode", "PlayMode") if args.platform == "both" else (args.platform,)
        for platform in platforms:
            xml = output / f"{platform}.xml"
            command = ["/usr/bin/sandbox-exec", "-f", str(profile), str(unity),
                       "-batchmode", "-nographics", "-forgetProjectPath", "-refreshImportMode", "InProcess",
                       "-projectPath", str(project), "-runTests",
                       "-testPlatform", platform, "-testResults", str(xml),
                       "-assemblyNames", "EditMode.Tests" if platform == "EditMode" else "Tests",
                       "-logFile", str(output / f"{platform}.log")]
            if args.filter:
                command.extend(["-testFilter", args.filter])
            suite = {"platform": platform, "command": command}
            result["suites"].append(suite)
            if args.prepare_only:
                suite["status"] = "prepared"
                continue
            started = time.monotonic()
            try:
                code = run_process(command, env, args.timeout)
                suite.update(summarize(xml, code))
            except subprocess.TimeoutExpired:
                suite.update(status="failed", error=f"Exceeded {args.timeout}s; process group terminated")
            suite["elapsed_seconds"] = round(time.monotonic() - started, 3)
            write_json(output / "summary.json", result)
            print(f"{platform}: {suite['status']} ({suite.get('passed', 0)} passed, "
                  f"{suite.get('failed', 0)} failed, {suite.get('skipped', 0)} skipped)", flush=True)
        result["status"] = "prepared" if args.prepare_only else (
            "passed" if all(s["status"] == "passed" for s in result["suites"]) else "failed")
    except (OSError, ValueError, KeyboardInterrupt) as error:
        result["error"] = str(error) or "Interrupted"
    finally:
        try:
            cleanup_native_fixture_paths(leases)
        except (OSError, ValueError) as error:
            result.update(status="failed", cleanup_error=str(error))
    write_json(output / "summary.json", result)
    if "error" in result:
        print(result["error"], file=sys.stderr)
    return 0 if result["status"] in ("passed", "prepared") else 1


if __name__ == "__main__":
    sys.exit(main())
