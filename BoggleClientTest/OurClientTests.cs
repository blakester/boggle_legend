// Authors: Blake Burton, Cameron Minkel
// Start date: 12/5/14

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BB;
using System.Net.Sockets;
using CustomNetworking;
using BoggleClient;
using System.Net;
using System.Text;

namespace BoggleClientTest
{
    [TestClass]
    public class OurClientTests
    {
        Model model;
        TcpListener server;
        StringSocket ss;

        event Action<string[]> StartMessageEvent; // Event when START is recieved.
        
        [TestMethod]
        public void TestConnect()
        {
            //// Create mock Boggle server and listen for clients.
            //server = new TcpListener(IPAddress.Any, 2000);
            //server.Start();

            //// Connect with a client and create StringSocket.
            //TcpClient client = new TcpClient("localhost", 2000); 
            //server.BeginAcceptSocket(AcceptSocketCallback, null);  
          
            //// Fire off a start message event
            //string[] startMessageTokens = { "START", "ABCDEFGHIJKLMNOP", "120", "Elvis" };
            //StartMessageEvent(startMessageTokens);

            model = new Model();
            //model.GameEndedEvent += TestGameEndResetEverything;
            //model.StartMessageEvent += GameStartMessage;
            //model.TimeMessageEvent += GameTimeMessage;              // CREATE TEST METHODS FOR THESE
            //model.ScoreMessageEvent += GameScoreMessage;
            //model.SummaryMessageEvent += GameSummaryMessage;
            //model.SocketExceptionEvent += GameSocketFail;

        }

        private void TestGameEndResetEverything(bool b)
        {

        }


        private void AcceptSocketCallback(IAsyncResult result)
        {
            // Create a StringSocket with the client.
            Socket s = server.EndAcceptSocket(result);
            ss = new StringSocket(s, Encoding.UTF8);
        }
    }
}
