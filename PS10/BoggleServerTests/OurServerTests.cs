//// Authors: Blake Burton, Cameron Minkel
//// Start date: 11/20/14

//using System;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using BB;
//using System.Threading;
//using System.Collections.Generic;
//using System.Net.Sockets;
//using System.Net;
//using CustomNetworking;
//using System.Text;
//using System.Text.RegularExpressions;

//namespace BoggleServerTests
//{
//    [TestClass]
//    public class OurServerTests
//    {
//        private bool startMessageSent = false;
//        private bool didCallbackHappen = false;
//        private bool player1stop = false;
//        private bool player2stop = false;
//        AutoResetEvent wait = new AutoResetEvent(false);

//        //////////////////////////////////////Good Server Constructor//////////////////////////////////////

//        //Tests a valid 2 contructor server start.
//        [TestMethod]
//        public void TestBoggleServerStartGood1()
//        {
//            string[] args = {"5", @"..\..\..\Resources\Resources\dictionary.txt"};
//            BoggleServer BS = new BoggleServer(args);
//            Thread.Sleep(500);
//            Assert.IsTrue(BoggleServer.GameLength == 5);
//            Assert.IsTrue(BoggleServer.LegalWords.SetEquals(
//                new HashSet<string>(System.IO.File.ReadAllLines(args[1]))));
//            Assert.IsTrue(BoggleServer.CustomBoard == null);
//            BS.CloseServer();

//        }

//        //Tests a valid 3 constructor server start.
//        [TestMethod]
//        public void TestBoggleServerStartGood2()
//        {
//            string[] args = { "5", @"..\..\..\Resources\Resources\dictionary.txt",
//                            "aaaaaaaaaaaaaaaa"};
//            BoggleServer BS = new BoggleServer(args);
//            Thread.Sleep(500);
//            Assert.IsTrue(BoggleServer.GameLength == 5);
//            Assert.IsTrue(BoggleServer.LegalWords.SetEquals(
//                new HashSet<string>(System.IO.File.ReadAllLines(args[1]))));
//            Assert.IsTrue(BoggleServer.CustomBoard == "aaaaaaaaaaaaaaaa");
//            BS.CloseServer();
//        }

//        ////////////////////////////////////Bad Server Constructor//////////////////////////////////////
//        //Tests bad constructor: Negative Time.
//        [TestMethod]
//        public void TestBoggleServerStartBad1()
//        {
//            string[] args = { "-5", @"..\..\..\Resources\Resources\dictionary.txt",
//                            "aaaaaaaaaaaaaaaa"};
//            BoggleServer BS = new BoggleServer(args);
//            Thread.Sleep(500);
//            Assert.IsTrue(BoggleServer.GameLength == -1);

//        }

//        //Tests bad constructor: 0 Time
//        [TestMethod]
//        public void TestBoggleServerStartBad2()
//        {
//            string[] args = { "0", @"..\..\..\Resources\Resources\dictionary.txt",
//                            "aaaaaaaaaaaaaaaa"};
//            BoggleServer BS = new BoggleServer(args);
//            Thread.Sleep(500);
//            Assert.IsTrue(BoggleServer.GameLength == -1);

//        }

//        //Tests bad constructor: Time with a Double.
//        [TestMethod]
//        public void TestBoggleServerStartBad3()
//        {
//            string[] args = { "60.5", @"..\..\..\Resources\Resources\dictionary.txt",
//                            "aaaaaaaaaaaaaaaa"};
//            BoggleServer BS = new BoggleServer(args);
//            Thread.Sleep(500);
//            Assert.IsTrue(BoggleServer.GameLength == -1);

//        }

//        //Tests bad constructors: String instead of int.
//        [TestMethod]
//        public void TestBoggleServerStartBad4()
//        {
//            string[] args = { "aa", @"..\..\..\Resources\Resources\dictionary.txt",
//                            "aaaaaaaaaaaaaaaa"};
//            args[0] = null;
//            BoggleServer BS = new BoggleServer(args);
//            Thread.Sleep(500);
//            Assert.IsTrue(BoggleServer.GameLength == -1);

//        }

//        //Tests bad constructor: Bad filepath, null.
//        [TestMethod]
//        public void TestBoggleServerStartBad5()
//        {
//            string[] args = { "60", @"..\..\..\Resources\Resources\dictionary.txt",
//                            "aaaaaaaaaaaaaaaa"};
//            args[1] = null;
//            BoggleServer BS = new BoggleServer(args);
//            Thread.Sleep(500);
//            Assert.IsTrue(BoggleServer.GameLength == -1);
//        }

//        //Tests bad constructor: Bad filepath, bad string.
//        [TestMethod]
//        public void TestBoggleServerStartBad6()
//        {
//            string[] args = { "60", @"Bad FilePath", "aaaaaaaaaaaaaaaa" };
//            BoggleServer BS = new BoggleServer(args);
//            Thread.Sleep(500);
//            Assert.IsTrue(BoggleServer.GameLength == -1);

//        }

//        //Tests bad constructor: Bad string, null.
//        [TestMethod]
//        public void TestBoggleServerStartBad7()
//        {
//            string[] args = { "0", @"..\..\..\Resources\Resources\dictionary.txt",
//                            "aaaaaaaaaaaaaaaa"};
//            args[2] = null;
//            BoggleServer BS = new BoggleServer(args);
//            Thread.Sleep(500);
//            Assert.IsTrue(BoggleServer.GameLength == -1);

//        }

//        //Tests bad constructor: Bad string, not 16 characters.
//        [TestMethod]
//        public void TestBoggleServerStartBad8()
//        {
//            string[] args = { "60", @"..\..\..\Resources\Resources\dictionary.txt", "Not 16 long" };
//            BoggleServer BS = new BoggleServer(args);
//            Thread.Sleep(500);
//            Assert.IsTrue(BoggleServer.GameLength == -1);

//        }

//        //Tests bad constructor: Bad string, not all letters.
//        [TestMethod]
//        public void TestBoggleServerStartBad9()
//        {
//            string[] args = { "60", @"..\..\..\Resources\Resources\dictionary.txt",
//                            "aaaaaaaaaaaaa123"};
//            BoggleServer BS = new BoggleServer(args);
//            Thread.Sleep(500);
//            Assert.IsTrue(BoggleServer.GameLength == -1);

//        }

//        //Tests bad constructor: Null args[]
//        [TestMethod]
//        public void TestBoggleServerStartBad10()
//        {
//            BoggleServer BS = new BoggleServer(null);
//            Assert.IsTrue(BoggleServer.GameLength == -1);

//        }

//        //Tests bad constructor: More than 3 args.
//        [TestMethod]
//        public void TestBoggleServerStartBad11()
//        {
//            string[] args = { "60", @"..\..\..\Resources\Resources\dictionary.txt",
//                            "aaaaaaaaaaaaaaaa", " "};
//            BoggleServer BS = new BoggleServer(args);
//            Thread.Sleep(500);
//            Assert.IsTrue(BoggleServer.GameLength == -1);

//        }

//        //Tests bad constructor: Only 1 arg.
//        [TestMethod]
//        public void TestBoggleServerStartBad12()
//        {
//            string[] args = { "60" };
//            BoggleServer BS = new BoggleServer(args);
//            Thread.Sleep(500);
//            Assert.IsTrue(BoggleServer.GameLength == -1);

//        }

//        //////////////////////////////////////ServerConnection//////////////////////////////////////

//        //This method tests to see if two clients can successfully connect, speak, and create
//        //a game with no problem.
//        [TestMethod]
//        public void TestServerConnectionGood()
//        {
//            // Connect two clients to the server using the below game settings.
//            string[] args = { "60", @"..\..\..\Resources\Resources\dictionary.txt"};
//            BoggleServer BS = new BoggleServer(args);
//            TcpClient client = new TcpClient("localhost", 2000);
//            TcpClient client2 = new TcpClient("localhost", 2000);

//            // Make sure the clients are connected.
//            Assert.IsTrue(client.Connected, "Did not connect.");
//            Assert.IsTrue(client2.Connected, "Did not connect.");

//            // Create StringSockets between the clients and the server.
//            StringSocket socket = new StringSocket(client.Client, UTF8Encoding.Default);
//            StringSocket socket2 = new StringSocket(client2.Client, UTF8Encoding.Default);

//            // Send a message that should be ignored.
//            socket.BeginSend("I want nachos!\n", ExpectTerminateMessage, socket);
//            wait.WaitOne(500);

//            // If didCallbackHappen is true, the correct "IGNORING" message.
//            // was sent
//            Assert.IsTrue(didCallbackHappen);
//            didCallbackHappen = false;

//            // Make sure that Game is started.
//            socket.BeginSend("PLAY Cameron\n", (e, o) => { }, null);
//            socket2.BeginSend("PLAY Blake\n", ListenForStartMessage, socket2);
//            wait.WaitOne(500);
//            Assert.IsTrue(startMessageSent);
//            didCallbackHappen = false;

//            // Make sure we are still connected.
//            Assert.IsTrue(client.Connected);
//            Assert.IsTrue(client2.Connected);

//            // Clean up
//            socket.Close();
//            socket2.Close();
//            client.Close();
//            client2.Close();
//            BS.CloseServer();
           
//        }

//        // Tests a complete legit game and ensure proper scores are sent back.
//        [TestMethod]
//        public void TestGame()
//        {
//            // Set up server.
//            string[] args = { "3", @"..\..\..\Resources\Resources\dictionary.txt",
//                            "catSDoGSQuAKCAke"};
//            BoggleServer BS = new BoggleServer(args);
//            TcpClient client = new TcpClient("localhost", 2000);
//            TcpClient client2 = new TcpClient("localhost", 2000);

//            StringSocket socket = new StringSocket(client.Client, UTF8Encoding.Default);
//            StringSocket socket2 = new StringSocket(client2.Client, UTF8Encoding.Default);

//            // Start game and send a bunch of words.
//            socket.BeginSend("PLAY Cameron\n", PlayerOneWords, socket);
//            socket.BeginSend("WORD CAT\n", (e, o) => { }, null);
//            socket2.BeginSend("PLAY Blake\n", PlayerTwoWords, socket2);

//            socket.BeginReceive(PlayerRec, socket);
//            socket.BeginReceive(PlayerRec, socket2);

//            wait.WaitOne(5000);
//            Assert.IsTrue(didCallbackHappen);
//            didCallbackHappen = false;
//            player1stop = false;
//            player2stop = false;

//            socket.Close();
//            socket2.Close();
//            client.Close();
//            client2.Close();
//            BS.CloseServer();
//        }

//        // Tests a complete legit game and ensure proper scores are sent back.
//        [TestMethod]
//        public void TestGame2()
//        {
//            // Set up server.
//            string[] args = { "3", @"..\..\..\Resources\Resources\dictionary.txt",
//                            "compretuplayspre"};
//            BoggleServer BS = new BoggleServer(args);
//            TcpClient client = new TcpClient("localhost", 2000);
//            TcpClient client2 = new TcpClient("localhost", 2000);

//            StringSocket socket = new StringSocket(client.Client, UTF8Encoding.Default);
//            StringSocket socket2 = new StringSocket(client2.Client, UTF8Encoding.Default);

//            // Start game and send a bunch of words.
//            socket.BeginSend("PLAY Cameron\n", PlayerOneWords2, socket);
//            socket2.BeginSend("PLAY Blake\n", PlayerTwoWords2, socket2);

//            socket.BeginReceive(PlayerRec2, socket);
//            socket.BeginReceive(PlayerRec2, socket2);

//            wait.WaitOne(5000);
//            Assert.IsTrue(didCallbackHappen);
//            didCallbackHappen = false;
//            player1stop = false;
//            player2stop = false;

//            socket.Close();
//            socket2.Close();
//            client.Close();
//            client2.Close();
//            BS.CloseServer();
//        }

//        // Callback that will check if end game message is correct.
//        private void PlayerRec(string s, Exception e, object payload)
//        {     
//            if (Regex.IsMatch(s.ToUpper(), @"^(STOP\s)"))
//            {
//                if (s.ToUpper() == "STOP 1 CATS 1 DOG 1 CAKE 1 1234 1 9876")
//                    player1stop = true;
//                if (s.ToUpper() == "STOP 1 DOG 1 CATS 1 CAKE 1 9876 1 1234")
//                    player2stop = true;
//                if (player1stop && player2stop)
//                    didCallbackHappen = true;
//            }

//            if (((StringSocket)payload).Connected)
//                ((StringSocket)payload).BeginReceive(PlayerRec, payload);

//        }

//        // Callback that will check if end game message is correct.
//        private void PlayerRec2(string s, Exception e, object payload)
//        {
//            if (Regex.IsMatch(s.ToUpper(), @"^(STOP\s)"))
//            {
//                if (s.ToUpper() == "STOP 1 COMPUTER 1 COMPUTE 2 PLAYER TARPS 0 0" ||
//                    s.ToUpper() == "STOP 1 COMPUTER 1 COMPUTE 2 TARPS PLAYER 0 0")
//                    player1stop = true;
//                if (s.ToUpper() == "STOP 1 COMPUTE 1 COMPUTER 2 PLAYER TARPS 0 0" ||
//                    s.ToUpper() == "STOP 1 COMPUTE 1 COMPUTER 2 TARPS PLAYER 0 0")
//                    player2stop = true;
//                if (player1stop && player2stop)
//                    didCallbackHappen = true;
//            }

//            if (((StringSocket)payload).Connected)
//                ((StringSocket)payload).BeginReceive(PlayerRec2, payload);

//        }

//        // Player one mass send words.
//        private void PlayerOneWords(Exception e, object payload)
//        {

//            Thread.Sleep(1000);
//            ((StringSocket)payload).BeginSend("WORD CATS\n", (ex, o) => { }, payload);
//            ((StringSocket)payload).BeginSend("WORD 1234\n", (ex, o) => { }, payload);
//            ((StringSocket)payload).BeginSend("WORD cake\n", (ex, o) => { }, payload);
//            ((StringSocket)payload).BeginSend("WORD cats\n", (ex, o) => { }, payload);
//            ((StringSocket)payload).BeginSend("WORD at\n", (ex, o) => { }, payload);
//            ((StringSocket)payload).BeginSend("cakeisbest\n", (ex, o) => { }, payload);
//        }

//        private void PlayerOneWords2(Exception e, object payload)
//        {

//            Thread.Sleep(1000);
//            ((StringSocket)payload).BeginSend("WORD computer\n", (ex, o) => { }, payload);
//            ((StringSocket)payload).BeginSend("WORD player\n", (ex, o) => { }, payload);
//            ((StringSocket)payload).BeginSend("WORD tarps\n", (ex, o) => { }, payload);

//        }

//        // Player two mass send words.
//        private void PlayerTwoWords(Exception e, object payload)
//        {

//            Thread.Sleep(1000);
//            ((StringSocket)payload).BeginSend("WORD DOG\n", (ex, o) => { }, payload);
//            ((StringSocket)payload).BeginSend("WORD 9876\n", (ex, o) => { }, payload);
//            ((StringSocket)payload).BeginSend("WORD cake\n", (ex, o) => { }, payload);
//            ((StringSocket)payload).BeginSend("WORD dog\n", (ex, o) => { }, payload);
//            ((StringSocket)payload).BeginSend("pieisbest\n", (ex, o) => { }, payload);
//        }

//        private void PlayerTwoWords2(Exception e, object payload)
//        {

//            Thread.Sleep(1000);
//            ((StringSocket)payload).BeginSend("WORD player\n", (ex, o) => { }, payload);
//            ((StringSocket)payload).BeginSend("WORD compute\n", (ex, o) => { }, payload);
//            ((StringSocket)payload).BeginSend("WORD tarps\n", (ex, o) => { }, payload);


//        }


//        // We sent a bad message and expect a IGNORING message
//        private void ExpectTerminateMessage(Exception e, object payload)
//        {

//            ((StringSocket)payload).BeginReceive(TerminateMessage, payload);
//        }


//        // Should get an IGNORING message
//        private void TerminateMessage(string s, Exception e, object payload)
//        {

//            if (s == "IGNORING I want nachos!")
//                didCallbackHappen = true;
//        }


//        // We sent a start message and expect a start in return
//        private void ListenForStartMessage(Exception e, object payload)
//        {

//            ((StringSocket)payload).BeginReceive(CheckForStartMessage, payload);
//        }


//        // Should get a START message with args
//        private void CheckForStartMessage(string s, Exception e, object payload)
//        {

//            if (s.Contains("START") && s.Contains("60") && s.Contains("Cameron"))
//                startMessageSent = true;
//        }
//    }
//}
