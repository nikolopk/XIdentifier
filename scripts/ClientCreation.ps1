function Average($array)
{
    $RunningTotal = 0;
    foreach($entry in $array){
        $RunningTotal += $entry
    }
    return (($RunningTotal) / ($array.Length));
}

$total_clients = 5
$execution_times = [System.Collections.Concurrent.ConcurrentBag[psobject]]::new()

$execution_times += 1..$total_clients | ForEach-Object -Parallel {
    $result = Start-Process -FilePath D:\VS_Projects\XIdentifier\XClient\bin\Debug\net7.0\XClient.exe -Wait -PassThru
    $local_execution_times = $using:execution_times
    $local_execution_times.Add($result.ExitCode)
    Write-Host "Time: $($result.ExitCode) ms"
} -ThrottleLimit 60

$average_time = Average($execution_times)
Write-Host "Loops: $total_clients, Executoin Time Average: $average_time ms"
