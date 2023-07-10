using System.Collections.Generic;
using System.Text.Json;

namespace Abstractions
{
    public static class MessageHelper
    {
        public static (StateInfo.TransmissionState state, List<DataEntry> entries) ParseMessage(string message)
        {
            var parsedMessage = JsonSerializer.Deserialize<StateInfo>(message);

            return (parsedMessage.State, parsedMessage.DataEntries);
        }

        public static string CreateMessage(StateInfo.TransmissionState state, List<DataEntry>? entries = null)
        {
            var objToSend = new StateInfo
            {
                State = state,
                DataEntries = entries ?? new List<DataEntry>()
            };

            return JsonSerializer.Serialize(objToSend);
        }
    }
}
