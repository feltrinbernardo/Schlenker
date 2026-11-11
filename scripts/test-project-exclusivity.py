"""Process-level lease tests; no native project or Siemens API is accessed."""
import importlib.util
import os
from pathlib import Path
import subprocess
import tempfile
import time
import unittest

ROOT = Path(__file__).resolve().parents[1]
PWSH = os.environ.get('PWSH', 'pwsh')
LEASE = ROOT / 'scripts/tia-project-lease.ps1'
def q(value): return "'" + str(value).replace("'", "''") + "'"
def run(command):
    return subprocess.run([PWSH, '-NoProfile', '-Command', command], text=True, capture_output=True, timeout=20)
def enter(project):
    return f". {q(LEASE)}; $l=Enter-TiaProjectLease {q(project)}; try {{ 'ENTERED' }} finally {{ Exit-TiaProjectLease $l }}"
def wait_file(path, process):
    deadline=time.monotonic()+15
    while not path.exists():
        if process.poll() is not None or time.monotonic()>deadline:
            raise AssertionError('holder did not become ready')
        time.sleep(.05)

class ExclusivityTests(unittest.TestCase):
    def test_process_ownership_and_crash(self):
        for crash in (False, True):
            with self.subTest(crash=crash), tempfile.TemporaryDirectory() as tmp:
                d=Path(tmp); project=d/'project.fixture'; ready=d/'ready'; release=d/'release'
                # A nonexistent fixture path is sufficient: lease code never opens it.
                cmd=f". {q(LEASE)}; $l=Enter-TiaProjectLease {q(project)}; try {{ [IO.File]::WriteAllText({q(ready)},$l.Path); while (!(Test-Path {q(release)})) {{ Start-Sleep -Milliseconds 50 }} }} finally {{ Exit-TiaProjectLease $l }}"
                holder=subprocess.Popen([PWSH,'-NoProfile','-Command',cmd],stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True)
                lease_path=None
                try:
                    wait_file(ready,holder); lease_path=Path(ready.read_text())
                    # Normalized relative segments must hit the same lease.
                    blocked=run(enter(d/'child'/'..'/'project.fixture'))
                    self.assertNotEqual(blocked.returncode,0)
                    self.assertIn('PROJECT_ACCESS_BLOCKED',blocked.stderr)
                    self.assertNotIn('ENTERED',blocked.stdout)
                    self.assertTrue(lease_path.exists())
                    self.assertEqual(run(enter(d/'other.fixture')).returncode,0)
                    if crash:
                        holder.kill(); holder.communicate(timeout=10)
                        self.assertTrue(lease_path.exists())
                        self.assertNotEqual(run(enter(project)).returncode,0)
                    else:
                        release.touch(); holder.communicate(timeout=10)
                        self.assertEqual(holder.returncode,0)
                        self.assertFalse(lease_path.exists())
                        self.assertEqual(run(enter(project)).returncode,0)
                    self.assertFalse(project.exists())
                finally:
                    if holder.poll() is None: holder.kill()
                    holder.communicate(timeout=10)
                    # Only this test's marker, after its owner is confirmed stopped.
                    if lease_path and lease_path.exists(): lease_path.unlink()

    def test_returned_exception_releases(self):
        with tempfile.TemporaryDirectory() as tmp:
            p=Path(tmp)/'fixture'
            result=run(f". {q(LEASE)}; $l=Enter-TiaProjectLease {q(p)}; try {{ throw 'simulated' }} finally {{ Exit-TiaProjectLease $l }}")
            self.assertNotEqual(result.returncode,0)
            self.assertEqual(run(enter(p)).returncode,0)

    def test_two_batch_runners_share_lease(self):
        spec=importlib.util.spec_from_file_location('batchtests',ROOT/'scripts/test-batch-transactions.py')
        mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod)
        import sys
        with tempfile.TemporaryDirectory() as tmp:
            d=Path(tmp); project=d/'project.fixture'; project.write_text('untouched')
            archive=d/'archive.fixture'; archive.write_text('untouched')
            build=d/'build';audit=d/'audit';ready=d/'ready';release=d/'release';calls=d/'calls'
            hold="import os,time\nfrom pathlib import Path\nPath(os.environ['READY']).touch()\nwhile not Path(os.environ['RELEASE']).exists(): time.sleep(.05)\n"
            build.write_text('#!'+sys.executable+'\n'+hold+mod.BUILD);build.chmod(0o700)
            audit.write_text('#!'+sys.executable+'\n'+mod.AUDIT);audit.chmod(0o700)
            env=os.environ.copy();env.update(READY=str(ready),RELEASE=str(release),CALLS=str(calls),CASE='success',RENDER='no')
            common=f'-ProjectPath {q(project)} -BuildExe {q(build)} -AuditExe {q(audit)} -Screens first'
            first=f"& {q(ROOT/'scripts/run-global-common-page-transactions.ps1')} {common} -OutputRoot {q(d/'out1')}"
            second=f"& {q(ROOT/'scripts/run-global-common-render-page-transactions.ps1')} {common} -CheckpointArchive {q(archive)} -OutputRoot {q(d/'out2')}"
            owner=subprocess.Popen([PWSH,'-NoProfile','-Command',first],env=env,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True)
            try:
                wait_file(ready,owner)
                result=subprocess.run([PWSH,'-NoProfile','-Command',second],env=env,text=True,capture_output=True,timeout=20)
                self.assertNotEqual(result.returncode,0)
                self.assertIn('PROJECT_ACCESS_BLOCKED',result.stderr)
                self.assertFalse((d/'out2').exists())
                release.touch();stdout,stderr=owner.communicate(timeout=20)
                self.assertEqual(owner.returncode,0,stdout+stderr)
                self.assertEqual(calls.read_text().splitlines(),['build','audit'])
                self.assertEqual(project.read_text(),'untouched')
            finally:
                release.touch()
                if owner.poll() is None: owner.communicate(timeout=20)

if __name__=='__main__': unittest.main()
