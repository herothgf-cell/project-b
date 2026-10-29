import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


class PvpRunnerTests(unittest.TestCase):
    def test_failed_invocation_removes_previous_success_result(self):
        # A build failure must not leave an earlier invocation's success artifact.
        with tempfile.TemporaryDirectory(prefix='pvp-runner-regression-') as temp:
            repo = Path(temp)
            tools = repo / 'tools'
            tools.mkdir()
            runner = tools / 'test_pvp.py'
            shutil.copyfile(Path(__file__).resolve().parents[1] / 'tools/test_pvp.py', runner)
            result = repo / 'artifacts/pvp-tests/result.txt'
            result.parent.mkdir(parents=True)
            result.write_text('PVP_ISOLATION_OK stale-success\n', encoding='utf-8')

            # Python interprets the runner's `build` argument as a missing script:
            # a real failing subprocess, with no SDK/network dependency or mock.
            completed = subprocess.run(
                [sys.executable, str(runner), '--dotnet', sys.executable],
                cwd=repo, capture_output=True, text=True, timeout=30,
            )

            self.assertNotEqual(completed.returncode, 0, completed.stdout + completed.stderr)
            self.assertFalse(result.exists(), 'A failed invocation retained stale success evidence')


if __name__ == '__main__':
    unittest.main()
