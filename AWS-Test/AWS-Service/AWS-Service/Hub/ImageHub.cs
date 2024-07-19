using Microsoft.AspNetCore.SignalR;

namespace AWS_Service
{
    public class ImageHub : Hub
    {
        public async Task GrupoImagenes(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }
    }
}
