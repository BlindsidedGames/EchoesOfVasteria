#!/usr/bin/env python3
"""Validate the existing static website before publishing it to GitHub Pages."""

from html.parser import HTMLParser
from pathlib import Path
import re
import sys
from urllib.parse import unquote, urlsplit


class References(HTMLParser):
    def __init__(self):
        super().__init__()
        self.urls = []

    def handle_starttag(self, tag, attrs):
        for key, value in attrs:
            if value and key in {"href", "src", "poster"}:
                self.urls.append(value)
            elif value and key == "srcset":
                self.urls.extend(part.strip().split()[0] for part in value.split(",") if part.strip())


def validate(site):
    site = Path(site).resolve()
    errors = []
    required = ("index.html", "privacy.html", "style.css", "script.js", "CNAME")
    for name in required:
        if not (site / name).is_file():
            errors.append(f"Missing required website file: {name}")
    if (site / "CNAME").is_file():
        if (site / "CNAME").read_text(encoding="utf-8").strip() != "echoesofvasteria.com":
            errors.append("CNAME must retain echoesofvasteria.com")
    if not (site / "images").is_dir():
        errors.append("Missing website images directory")

    allowed = {".html", ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg", ".ico", ".avif", ".woff", ".woff2", ".ttf", ".txt", ".xml"}
    files = []
    checked = 0
    for path in sorted(site.rglob("*")):
        relative = path.relative_to(site)
        if path.is_symlink():
            errors.append(f"Symlinks are not allowed in the published artifact: {relative}")
            continue
        if any(part.startswith(".") for part in relative.parts):
            errors.append(f"Hidden content is not allowed in the published artifact: {relative}")
            continue
        if not path.is_file():
            continue
        files.append(path)
        if path.name != "CNAME" and path.suffix.lower() not in allowed:
            errors.append(f"Unexpected non-website file: {relative}")
        if path.suffix.lower() not in {".html", ".css", ".js"}:
            continue
        try:
            content = path.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            errors.append(f"Website text is not valid UTF-8: {relative}")
            continue
        urls = []
        if path.suffix.lower() == ".html":
            parser = References()
            parser.feed(content)
            urls = parser.urls
        elif path.suffix.lower() == ".css":
            urls = re.findall(r"url\(\s*['\"]?([^)'\"\s]+)['\"]?\s*\)", content)
        for url in urls:
            parsed = urlsplit(url)
            if parsed.scheme or parsed.netloc or not parsed.path:
                continue
            local_path = unquote(parsed.path)
            target = ((site / local_path.lstrip("/")) if local_path.startswith("/") else (path.parent / local_path)).resolve()
            if not target.is_relative_to(site):
                errors.append(f"Reference escapes the website: {relative} -> {url}")
                continue
            if target.is_dir():
                target /= "index.html"
            if not target.is_file():
                errors.append(f"Missing local target: {relative} -> {url}")
            checked += 1
    return errors, len(files), checked


if __name__ == "__main__":
    root = Path(__file__).resolve().parents[2] / "docs/archive/website"
    failures, file_count, reference_count = validate(root)
    if failures:
        print("Website validation failed:\n" + "\n".join(f"- {error}" for error in failures), file=sys.stderr)
        raise SystemExit(1)
    print(f"Validated {file_count} website files and {reference_count} local references; domain and publication boundary preserved.")
