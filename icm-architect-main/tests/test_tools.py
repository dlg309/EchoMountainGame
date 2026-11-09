import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


TOOLS = Path(__file__).resolve().parents[1] / "scripts"


class ToolTests(unittest.TestCase):
    def setUp(self):
        folder = tempfile.TemporaryDirectory()
        self.addCleanup(folder.cleanup)
        self.root = Path(folder.name)

    def write(self, path, content):
        path = self.root / path
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding="utf-8")
        return path

    def run_tool(self, name, *args):
        return subprocess.run(
            [sys.executable, "-I", str(TOOLS / name), *map(str, args)],
            capture_output=True, text=True, encoding="utf-8", check=False,
        )

    def test_missing_reference_with_spaces_fails(self):
        self.write("CONTEXT.md", "# Stage\n\n## Inputs\n- Reference (every run): `docs/missing reference.md`\n")
        report = self.run_tool("links.py", self.root)
        self.assertEqual(report.returncode, 1, report.stdout)
        self.assertIn("missing reference.md", report.stdout)

    def test_working_path_under_reference_named_folder_can_be_pending(self):
        self.write("reference-notes/.keep", "")
        self.write("CONTEXT.md", "# Stage\n\n## Inputs\n- Working (this run): `reference-notes/pending.md`\n")
        report = self.run_tool("links.py", self.root)
        self.assertEqual(report.returncode, 0, report.stdout)

    def test_missing_workspace_fails_and_skip_excludes_subtree(self):
        self.write("Ignored/inner/note.md", "[bad link](missing.md)")
        self.write("active.md", "# Active")
        report = self.run_tool("links.py", self.root, "--skip", "ignored")
        self.assertEqual(report.returncode, 0, report.stdout)
        self.assertIn("1 files", report.stdout)
        for tool in ("links.py", "quotes.py"):
            report = self.run_tool(tool, self.root / "missing")
            self.assertNotEqual(report.returncode, 0)
            self.assertIn("workspace is not a directory", report.stderr)

    def test_nested_fence_example_is_skipped(self):
        self.write("guide.md", "# Guide\n\n````md\n```\n[example](missing.md)\n```\n````\n")
        report = self.run_tool("links.py", self.root)
        self.assertEqual(report.returncode, 0, report.stdout)

    def test_partial_word_cannot_pass_for_quote(self):
        self.write("sources/raw/talk.txt", "We concatenate the lists.")
        self.write("note.md", '# Note\n\n## In their words\n- "cat" (talk)\n')
        report = self.run_tool("quotes.py", self.root)
        self.assertEqual(report.returncode, 1, report.stdout)

    def test_unicode_words_are_verified_instead_of_discarded(self):
        self.write("sources/raw/talk.txt", "你好\nCafé au lait.")
        self.write("note.md", '# Note\n\n## In their words\n- "你好" (talk)\n- "Café au lait" (talk)\n')
        self.assertEqual(self.run_tool("quotes.py", self.root).returncode, 0)
        self.write("note.md", '# Note\n\n## In their words\n- "再见" (talk)\n')
        report = self.run_tool("quotes.py", self.root)
        self.assertEqual(report.returncode, 1, report.stdout)

    def test_alias_cannot_read_outside_raw_sources(self):
        self.write("sources/outside.txt", "This text is outside raw.")
        self.write("_meta/sources.txt", "outside = ../outside.txt\n")
        self.write("note.md", '# Note\n\n## In their words\n- "This text is outside raw" (outside)\n')
        report = self.run_tool("quotes.py", self.root)
        self.assertEqual(report.returncode, 1, report.stdout)
        self.assertIn("is not in sources/raw/", report.stdout)

    def test_empty_cut_and_reversed_page_range_fail(self):
        self.write("sources/raw/report.txt", "Page 1\nA valid first page.\nPage 2\nA valid second page.\n")
        self.write("note.md", '# Note\n\n## In their words\n- "…" (report page 1)\n- "A valid" (report pp 2-1)\n')
        report = self.run_tool("quotes.py", self.root)
        self.assertEqual(report.returncode, 1, report.stdout)
        self.assertIn("2 problem(s)", report.stdout)

    def test_transcript_keeps_wrapped_paragraph_text(self):
        self.write("sources/raw/talk.txt", "[0:15] We read the\nfull record before deciding.\n[0:30] Next topic.")
        self.write("note.md", '# Note\n\n## In their words\n- "We read the full record before deciding" (talk 0:15)\n')
        report = self.run_tool("quotes.py", self.root)
        self.assertEqual(report.returncode, 0, report.stdout)

    def test_cut_cannot_hide_negation_on_a_wrapped_line(self):
        self.write("sources/raw/talk.txt", "[0:15] We can\nnot\n[0:17] skip this check.")
        self.write("note.md", '# Note\n\n## In their words\n- "We can ... skip this check" (talk 0:15-0:17)\n')
        report = self.run_tool("quotes.py", self.root)
        self.assertEqual(report.returncode, 1, report.stdout)
        self.assertIn("drops a negation", report.stdout)

    def test_keep_out_matches_canonically_equivalent_text(self):
        self.write("_meta/keep-out.txt", "Café\n")
        self.write("note.md", "# Note\n\nCafé")
        report = self.run_tool("quotes.py", self.root)
        self.assertEqual(report.returncode, 1, report.stdout)
        self.assertIn("keep-out.txt", report.stdout)

    def test_graph_title_and_note_cannot_close_the_data_script(self):
        title = "Workspace </script> __DATA__"
        self.write("AGENTS.md", f"# {title}\n")
        self.write("note.md", "# Note\n\n</script>\n")
        graph = self.root / "graph.jsonl"
        self.assertEqual(self.run_tool("links.py", self.root, "--graph", graph).returncode, 0)
        report = self.run_tool("viz.py", graph)
        self.assertEqual(report.returncode, 0, report.stderr)
        page = (self.root / "viz.html").read_text(encoding="utf-8")
        name = page.split("window.BUNDLE_NAME = ", 1)[1].split(";\n", 1)[0]
        self.assertNotIn("</script>", name)
        self.assertEqual(json.loads(name), title)


if __name__ == "__main__":
    unittest.main()
