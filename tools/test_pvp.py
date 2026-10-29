#!/usr/bin/env python3
"""Compile/run pure PVP behavior tests and two real diagnostic client processes.
Requires .NET SDK 8+. This is not a Unity Editor/player validation command.
"""
import argparse
import os
from pathlib import Path
import subprocess
import tempfile
from xml.sax.saxutils import escape


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet', default=os.environ.get('PVP_DOTNET', 'dotnet'))
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[1]
    base = repo / 'unity/EungyeTaxi/Assets/Pvp'
    sources = [base / 'Runtime/Core/PvpSessionConfig.cs', base / 'Runtime/Core/PvpBuildTransaction.cs',
               base / 'Runtime/Persistence/PvpLocalStore.cs', base / 'Tests/EditMode/PvpIsolationTests.cs',
               repo / 'tests/PvpIsolationTests.cs']
    output = repo / 'artifacts/pvp-tests'
    output.mkdir(parents=True, exist_ok=True)
    # A failed run must not inherit an earlier invocation's success evidence.
    (output / 'result.txt').unlink(missing_ok=True)
    env = dict(os.environ, DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',
               DOTNET_GENERATE_ASPNET_CERTIFICATE='false')
    with tempfile.TemporaryDirectory(prefix='pvp-isolation-run-') as temp:
        temp = Path(temp)
        project = temp / 'PvpIsolation.csproj'
        project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
                           '<TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems>'
                           '<LangVersion>9.0</LangVersion></PropertyGroup><ItemGroup>' +
                           ''.join('<Compile Include="' + escape(str(p), {'"': '&quot;'}) + '"/>' for p in sources) +
                           '</ItemGroup></Project>', encoding='utf-8')
        subprocess.run([args.dotnet, 'build', str(project), '-o', str(output), '--nologo'], env=env, check=True)
        command = [args.dotnet, str(output / 'PvpIsolation.dll')]
        subprocess.run(command, env=env, check=True)
        store = temp / 'local-data'
        campaign = store / 'Taxi Prototype/은계 택시 시뮬레이터'
        campaign.mkdir(parents=True)
        sentinel = campaign / 'campaign.json'
        sentinel.write_bytes(b'campaign-sentinel')
        clients = [subprocess.Popen(command + ['--diagnostic', str(store)], env=env,
                                    stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True) for _ in range(2)]
        paths = []
        for client in clients:
            stdout, stderr = client.communicate(timeout=30)
            if client.returncode != 0:
                raise RuntimeError('Diagnostic child failed: ' + stderr)
            paths.append(Path(stdout.strip()))
        if paths[0] == paths[1] or any(p.parent != store / 'PvpPrototype' for p in paths):
            raise AssertionError('Two client processes did not allocate isolated distinct files')
        contents = [p.read_text(encoding='utf-8') for p in paths]
        if len(set(contents)) != 2 or any(not text.startswith('client-process=') for text in contents):
            raise AssertionError('Both clients must retain their own diagnostic contents')
        if sentinel.read_bytes() != b'campaign-sentinel' or len(list(campaign.iterdir())) != 1:
            raise AssertionError('Campaign sentinel modified')
        print('PVP_TWO_PROCESS_DIAGNOSTICS_OK clients=2 distinct=2 campaign=unchanged')
        (output / 'result.txt').write_text('PVP_ISOLATION_OK cases=8\nPVP_TWO_PROCESS_DIAGNOSTICS_OK clients=2 distinct=2 campaign=unchanged\n', encoding='utf-8')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
