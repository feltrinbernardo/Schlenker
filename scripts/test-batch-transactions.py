"""Offline subprocess contract tests. Requires Python 3 and pwsh on a POSIX host; never opens an engineering project."""
import os
import sys
from pathlib import Path
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
PWSH = os.environ.get('PWSH', 'pwsh')
BUILD = r"""
import os, sys
with open(os.environ['CALLS'], 'a') as f: f.write('build\n')
case=os.environ['CASE']
print('PHASE=ATTACH_STARTED', flush=True)
if case in ('security','timeout'):
    print('FAILURE_CODE='+('OPENNESS_SECURITY' if case=='security' else 'TIMEOUT')+'|PHASE=ATTACH_STARTED',file=sys.stderr,flush=True)
    sys.exit(1)
if case=='unknown': sys.exit(0)
print('COMPILE_ERRORS='+('1' if case=='compile' else '0'))
print('COMPILE_WARNINGS=0')
if case=='save': print('PHASE=SAVE_STARTED'); sys.exit(1)
if case=='compile': sys.exit(1)
print('STATUS=PASS')
"""
AUDIT = r"""
import os, sys
with open(os.environ['CALLS'], 'a') as f: f.write('audit\n')
case=os.environ['CASE']
if os.environ['RENDER']=='yes': screen,report=sys.argv[2:4]
else: report,screen=sys.argv[2:4]
if case=='audit': sys.exit(1)
if case=='wrong-screen': screen='other'
with open(report,'w') as f: f.write('Screen\tStatus\tIssueCount\n'+screen+'\tVALIDATED\t0\n')
"""

class BatchTests(unittest.TestCase):
    def test_contracts(self):
        for render in (False, True):
            for case in ('success','security','timeout','save','compile','unknown','audit','wrong-screen','prior'):
                with self.subTest(render=render, case=case), tempfile.TemporaryDirectory() as tmp:
                    d=Path(tmp); build=d/'build'; audit=d/'audit'
                    build.write_text('#!'+sys.executable+'\n'+BUILD); audit.write_text('#!'+sys.executable+'\n'+AUDIT)
                    build.chmod(0o700); audit.chmod(0o700)
                    # Deliberately not an ap19: scripts only check file existence.
                    project=d/'project.fixture'; project.write_bytes(b'untouched')
                    archive=d/'checkpoint.fixture'; archive.write_bytes(b'untouched')
                    out=d/'out'; calls=d/'calls.txt'
                    if case=='prior':
                        out.mkdir()
                        (out/'completion-ledger.tsv').write_text('Screen\tStatus\nfirst\tVALIDATED\n')
                    script=ROOT/'scripts'/('run-global-common-render-page-transactions.ps1' if render else 'run-global-common-page-transactions.ps1')
                    env=os.environ.copy(); env.update(CASE=case, CALLS=str(calls), RENDER='yes' if render else 'no')
                    # Invoke in a child pwsh, preserving the script's exit code.
                    def q(p): return "'"+str(p).replace("'","''")+"'"
                    command=f'& {q(script)} -ProjectPath {q(project)} -BuildExe {q(build)} -AuditExe {q(audit)} -OutputRoot {q(out)} -Screens first,second'
                    if render: command+=f' -CheckpointArchive {q(archive)}'
                    r=subprocess.run([PWSH,'-NoProfile','-Command',command],env=env,text=True,capture_output=True,timeout=30)
                    observed=calls.read_text().splitlines() if calls.exists() else []
                    expected=['build','audit','build','audit'] if case=='success' else [] if case=='prior' else ['build','audit'] if case in ('audit','wrong-screen') else ['build']
                    self.assertEqual(observed,expected,r.stdout+r.stderr)
                    self.assertEqual(r.returncode==0,case=='success',r.stdout+r.stderr)
                    self.assertEqual(project.read_bytes(),b'untouched')
                    if case=='success': self.assertIn('RUN_STATUS=PASS',r.stdout)
                    else: self.assertNotIn('RUN_STATUS=PASS',r.stdout)
                    if case in ('security','timeout'):
                        self.assertIn('FAILURE_CODE=',(out/'pages'/'first'/'apply-compile.err.log').read_text())

if __name__ == '__main__': unittest.main()
