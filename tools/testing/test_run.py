"""Runner contracts: false-green prevention and disposable input ownership.

Run with: python3 -m unittest discover -s tools/testing -p 'test_*.py' -v
No Unity, game saves, network or third-party Python packages are needed.
"""
from contextlib import redirect_stdout, redirect_stderr
import base64
import hashlib
import io
import json
from pathlib import Path
import tempfile
import subprocess
import sys
import time
import socket
import unittest
from unittest.mock import patch

import run


class RunnerContracts(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="eov-runner-contract-", dir="/tmp")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)

    def report(self, content, code=0):
        xml = self.root / "results.xml"
        xml.write_text(content)
        return run.summarize(xml, code)

    def test_pass_and_skip_are_counted_without_counting_suites(self):
        report = self.report('<test-run result="Passed"><test-suite result="Passed">'
                             '<test-case result="Passed"/><test-case result="Skipped"/>'
                             '</test-suite></test-run>')
        self.assertEqual((report["status"], report["passed"], report["skipped"]), ("passed", 1, 1))

    def test_missing_malformed_empty_and_all_skipped_results_fail(self):
        self.assertEqual(run.summarize(self.root / "missing.xml")["status"], "failed")
        for xml in ('<broken', '<test-run result="Passed"/>',
                    '<test-run result="Passed"><test-case result="Skipped"/></test-run>',
                    '<unrelated><test-case result="Passed"/></unrelated>'):
            with self.subTest(xml=xml):
                self.assertEqual(self.report(xml)["status"], "failed")

    def test_setup_failure_and_process_crash_cannot_be_hidden_by_passing_case(self):
        xml = ('<test-run result="Failed"><test-suite fullname="Fixture" result="Failed">'
               '<failure><message>setup exploded</message><stack-trace>at Setup</stack-trace></failure>'
               '<test-case result="Passed"/></test-suite></test-run>')
        report = self.report(xml)
        self.assertEqual(report["status"], "failed")
        self.assertEqual(report["failures"][0]["message"], "setup exploded")
        self.assertEqual(report["failures"][0]["name"], "Fixture")
        self.assertEqual(self.report('<test-run result="Passed"><test-case result="Passed"/></test-run>',
                                     code=1)["status"], "failed")

    def test_failed_leaf_retains_failure_details(self):
        report = self.report('<test-run result="Failed"><test-case fullname="Rewards.OnePayment" '
                             'result="Failed"><failure><message>paid twice</message>'
                             '</failure></test-case></test-run>')
        self.assertEqual(report["failed"], 1)
        self.assertEqual(report["failures"][0]["name"], "Rewards.OnePayment")
        self.assertEqual(report["failures"][0]["message"], "paid twice")

    def project(self):
        source = self.root / "source"
        for name in ("Assets", "Packages", "ProjectSettings", "Library"):
            (source / name).mkdir(parents=True)
        (source / "Assets/Input.cs").write_text("dirty input")
        (source / "Library/real-save.dat").write_text("must not copy")
        (source / "ProjectSettings/ProjectVersion.txt").write_text("m_EditorVersion: 6000.6.0f1\n")
        (source / "ProjectSettings/ProjectSettings.asset").write_text(
            "PlayerSettings:\n  companyName: RealCompany\n  productName: RealGame\n")
        return source

    def test_prepare_snapshots_dirty_input_without_unity_or_source_mutation(self):
        source = self.project()
        original = (source / "ProjectSettings/ProjectSettings.asset").read_bytes()
        output = self.root / "run with spaces"
        with patch.object(run, "run_process", side_effect=AssertionError("Must not launch Unity")), \
                redirect_stdout(io.StringIO()):
            code = run.main(["--project", str(source), "--output", str(output),
                             "--platform", "both", "--prepare-only", "--filter", "ResourceDropTests"])
        self.assertEqual(code, 0)
        self.assertEqual((source / "ProjectSettings/ProjectSettings.asset").read_bytes(), original)
        self.assertEqual((output / "project/Assets/Input.cs").read_text(), "dirty input")
        self.assertFalse((output / "project/Library/real-save.dat").exists())
        summary = json.loads((output / "summary.json").read_text())
        self.assertEqual(summary["status"], "prepared")
        self.assertEqual([s["platform"] for s in summary["suites"]], ["EditMode", "PlayMode"])
        self.assertEqual([s["command"][s["command"].index("-assemblyNames") + 1]
                          for s in summary["suites"]], ["EditMode.Tests", "Tests"])
        self.assertIn("EOVTests_", (output / "project/ProjectSettings/ProjectSettings.asset").read_text())
        self.assertIn("Assets/Input.cs", json.loads((output / "inputs.json").read_text()))
        self.assertIn("/usr/bin/sandbox-exec", summary["suites"][0]["command"])
        self.assertIn("(deny network*)", (output / "sandbox.sb").read_text())
        # Reusing a run must not turn stale XML into successful fresh evidence.
        with redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            run.main(["--project", str(source), "--output", str(output), "--prepare-only"])

    def test_snapshot_rejects_external_links_and_output_inside_source(self):
        source = self.project()
        with redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            run.main(["--project", str(source), "--output", str(source / "run"), "--prepare-only"])
        (source / "Assets/linked-save").symlink_to(source / "Library/real-save.dat")
        output = self.root / "run"
        with redirect_stdout(io.StringIO()), redirect_stderr(io.StringIO()):
            self.assertEqual(run.main(["--project", str(source), "--output", str(output),
                                       "--prepare-only"]), 1)
        self.assertFalse((output / "project/Assets/linked-save").exists())

    def test_offline_packages_preserve_locked_bytes_and_reject_missing_or_wrong_versions(self):
        source = self.project()
        locked = {"dependencies": {"com.example.test": {"version": "1.2.3", "source": "registry"}}}
        (source / "Packages/packages-lock.json").write_text(json.dumps(locked))
        with self.assertRaisesRegex(ValueError, "missing locked caches"):
            run.embed_cached_packages(source, source)
        package = source / "Library/PackageCache/com.example.test@hash"
        package.mkdir(parents=True)
        manifest = package / "package.json"
        manifest.write_text(json.dumps({"name": "com.example.test", "version": "1.2.4"}))
        with self.assertRaisesRegex(ValueError, "differs from lock"):
            run.embed_cached_packages(source, source)
        manifest.write_text(json.dumps({"name": "com.example.test", "version": "1.2.3"}))
        (package / "Code.cs").write_text("locked dependency bytes")
        destination = self.root / "snapshot"
        run.snapshot(source, destination)
        evidence = run.embed_cached_packages(source, destination)
        self.assertEqual((destination / "Packages/com.example.test/Code.cs").read_bytes(),
                         (package / "Code.cs").read_bytes())
        self.assertFalse((source / "Packages/com.example.test").exists())
        self.assertEqual(evidence["com.example.test"]["version"], "1.2.3")
        self.assertIn("Code.cs", evidence["com.example.test"]["files"])

    def test_native_fixture_links_route_to_scratch_and_preserve_existing_namespaces(self):
        home = self.root / "fake-home"
        scratch = self.root / "run"
        home.mkdir()
        scratch.mkdir()
        leases = {"links": [], "parents": [], "prefs": []}
        run.create_native_fixture_paths(home, scratch, "EOVTests_fixture", leases)
        native = home / "Library/Application Support/EchoesTests/EOVTests_fixture"
        (native / "save.bin").write_text("test progression")
        self.assertEqual((scratch / "native/saves/save.bin").read_text(), "test progression")
        run.cleanup_native_fixture_paths(leases)
        self.assertFalse(native.exists())
        self.assertTrue((scratch / "native/saves/save.bin").is_file())
        native.mkdir(parents=True)
        (native / "existing-save.bin").write_text("preserve")
        with self.assertRaisesRegex(ValueError, "pre-existing native"):
            run.create_native_fixture_paths(home, scratch, "EOVTests_fixture",
                                            {"links": [], "parents": [], "prefs": []})
        self.assertEqual((native / "existing-save.bin").read_text(), "preserve")

    def test_timeout_stops_workers_before_they_can_write_late_results(self):
        late = self.root / "late-result"
        child = "import time; from pathlib import Path; time.sleep(.5); Path(%r).write_text('late')" % str(late)
        parent = "import subprocess,sys,time; subprocess.Popen([sys.executable, '-c', %r]); time.sleep(10)" % child
        with self.assertRaises(subprocess.TimeoutExpired):
            run.run_process([sys.executable, "-c", parent], run.os.environ.copy(), .2)
        time.sleep(.6)
        self.assertFalse(late.exists(), "Timed-out worker wrote after runner returned")

    @unittest.skipUnless(sys.platform == "darwin", "macOS sandbox boundary")
    def test_os_sandbox_allows_disposable_writes_but_blocks_external_write_and_network(self):
        allowed = self.root / "allowed"
        allowed.mkdir()
        protected = self.root / "protected"
        protected.write_text("player progression sentinel")
        profile = allowed / "sandbox.sb"
        profile.write_text(run.sandbox_profile(allowed))
        pipe_name = base64.b64encode(hashlib.sha256(str(self.root).encode()).digest()).decode().replace("/", "_").rstrip("=")
        roslyn_path = self.root / pipe_name
        server = socket.socket(socket.AF_UNIX)
        server.bind(str(roslyn_path))
        server.listen(1)
        self.addCleanup(server.close)
        self.addCleanup(roslyn_path.unlink)
        script = """
import json, socket, sys
from pathlib import Path
Path(sys.argv[1]).write_text('test fixture')
with socket.socket(socket.AF_UNIX) as ipc:
    ipc.bind(sys.argv[3])
Path(sys.argv[3]).unlink()
with socket.socket() as server:
    server.bind(('127.0.0.1', 0))
    server.listen(1)
    with socket.create_connection(server.getsockname(), timeout=2) as client:
        connection, address = server.accept()
        connection.close()
blocked = []
try:
    Path(sys.argv[2]).write_text('overwrite')
except PermissionError:
    blocked.append('write')
try:
    with socket.socket() as connection:
        connection.settimeout(2)
        connection.connect(('192.0.2.1', 9))
except PermissionError:
    blocked.append('network')
try:
    with socket.socket(socket.AF_UNIX) as compiler:
        compiler.connect(sys.argv[4])
except PermissionError:
    blocked.append('shared-compiler')
print(json.dumps(blocked))
"""
        process = subprocess.run(["/usr/bin/sandbox-exec", "-f", str(profile), sys.executable,
                                  "-c", script, str(allowed / "fixture"), str(protected),
                                  str(self.root / "native-compiler.sock"), str(roslyn_path)],
                                 capture_output=True, text=True, timeout=10)
        self.assertEqual(process.returncode, 0, process.stderr)
        self.assertEqual(json.loads(process.stdout), ["write", "network", "shared-compiler"])
        self.assertEqual(protected.read_text(), "player progression sentinel")
        self.assertEqual((allowed / "fixture").read_text(), "test fixture")


if __name__ == "__main__":
    unittest.main()
