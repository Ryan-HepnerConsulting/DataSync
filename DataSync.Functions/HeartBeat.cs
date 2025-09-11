using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DataSync.Functions;

public class HeartBeat
{
    private readonly ILogger<HeartBeat> _logger;

    public HeartBeat(ILogger<HeartBeat> logger)
    {
        _logger = logger;
    }

    [Function("HeartBeat")]
    public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation($"Heart Beat - C# Timer trigger function executed at: {DateTime.Now}");

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation($"Next Heart Beat schedule at: {myTimer.ScheduleStatus.Next}");
            
        }
    }
}