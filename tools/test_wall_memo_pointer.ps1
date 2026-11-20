param([string]$Executable)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$Executable){$Executable=Join-Path $root 'builds/EungyeTaxi/EungyeTaxi.exe'}
$output=Join-Path $root ('artifacts/wall-memo-pointer/'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output -Force | Out-Null
$log=Join-Path $output 'player.log'
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Text;
public static class MemoPlayerWindow {
    delegate bool Enumerate(IntPtr window, IntPtr data);
    [DllImport("user32.dll")] static extern bool EnumWindows(Enumerate callback, IntPtr data);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint pid);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr window, StringBuilder text, int size);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr window, uint message, IntPtr flags, IntPtr point);
    [StructLayout(LayoutKind.Sequential)] public struct Area {public int left,top,right,bottom;}
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr window, out Area rect);
    public static IntPtr Find(uint pid) {
        IntPtr result=IntPtr.Zero;
        EnumWindows((window, data)=>{
            uint owner; GetWindowThreadProcessId(window,out owner);
            if(owner!=pid)return true;
            var name=new StringBuilder(128);GetClassName(window,name,name.Capacity);
            if(name.ToString()!="UnityWndClass")return true;
            result=window;return false;
        },IntPtr.Zero);
        return result;
    }
}
'@
$arguments=@('-fatherStoryCheck','-wallMemoPointerCheck','-force-d3d11','-screen-fullscreen','0','-shiftCaptureOutput',('"' + $output + '"'),'-logFile',('"' + $log + '"'))
$player=Start-Process $Executable -ArgumentList $arguments -WorkingDirectory $root -WindowStyle Hidden -PassThru
try {
    foreach($memo in @(@('car',238,149),@('none',606,149),@('home',422,142))){
        $deadline=[datetime]::UtcNow.AddSeconds(180)
        $marker='WALL_MEMO_AWAIT '+$memo[0]
        while(!(Test-Path $log) -or !(Select-String -LiteralPath $log -SimpleMatch $marker -Quiet)){
            if($player.HasExited -or [datetime]::UtcNow -gt $deadline){throw "Expected $marker in $log"}
            Start-Sleep -Milliseconds 200
        }
        $window=[MemoPlayerWindow]::Find($player.Id)
        $rect=New-Object MemoPlayerWindow+Area
        if(!$window -or ![MemoPlayerWindow]::GetClientRect($window,[ref]$rect)){throw 'Unity test window unavailable'}
        $scale=[Math]::Min($rect.right/1200.0,$rect.bottom/720.0)
        $x=[int](($rect.right-1200*$scale)/2+$memo[1]*$scale)
        $y=[int](($rect.bottom-720*$scale)/2+$memo[2]*$scale)
        $point=[IntPtr](($y -shl 16) -bor $x)
        foreach($message in @(512,513,514)){
            $flags=if($message -eq 513){[IntPtr]1}else{[IntPtr]::Zero}
            if(![MemoPlayerWindow]::PostMessage($window,$message,$flags,$point)){throw 'Pointer dispatch failed'}
            Start-Sleep -Milliseconds 150
        }
    }
    if(!$player.WaitForExit(40000) -or $player.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -SimpleMatch 'WALL_MEMO_POINTER_OK' -Quiet)){throw "Wall memo check failed. $log"}
    Get-Content $log | Select-String 'wall pointer selects|camera switch keeps|WALL_MEMO_POINTER_OK'
} finally {
    if(!$player.HasExited){$player.Kill()}
}
