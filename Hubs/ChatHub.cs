using Cold_Storage_GO.Models;
using Google;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MySqlX.XDevAPI;

namespace Cold_Storage_GO.Hubs
{
    public class ChatHub : Hub
    {
        private readonly DbContexts _context;

        public ChatHub(DbContexts context)
        {
            _context = context;
        }


        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Client connected: {Context.ConnectionId}");
            await Clients.All.SendAsync("ReceiveMessage", $"{Context.ConnectionId} has joined");
        }


    }


}
