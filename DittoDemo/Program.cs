﻿using System;
using DittoSDK;
using System.Collections.Generic;
using System.IO;

namespace Tasks
{
    class Program
    {
        static Ditto ditto;
        static bool isAskedToExit = false;
        static List<Task> tasks = new List<Task>();
        static DittoLiveQuery liveQuery;
        static DittoSubscription subscription;


        public static void Main(params string[] args)
        {
            //communicating with the big peer with APPId and Lichense Token
            ditto = new Ditto(DittoIdentity.OnlinePlayground("5a692dee-0efc-4628-9f17-696ba78f08cd", "e96e2791-f028-4179-9306-97ab14bad935"));
            //ditto = new Ditto(identity: DittoIdentity.OfflinePlayground());

            try
            {
                //ditto.SetOfflineOnlyLicenseToken("e96e2791-f028-4179-9306-97ab14bad935");
                DittoTransportConfig transportConfig = new DittoTransportConfig();

                // Enable Local Area Network Connections
                transportConfig.EnableAllPeerToPeer();

                //Or enable/disable each transport separately
                //BluetoothLe
                transportConfig.PeerToPeer.BluetoothLE.Enabled = true;
                transportConfig.PeerToPeer.BluetoothLE.Enabled = true;

                //Local Area Network
                transportConfig.PeerToPeer.Lan.Enabled = true;
                //Awdl
                transportConfig.PeerToPeer.Awdl.Enabled = true;

                // Listen for incoming connections on port 4000
                transportConfig.Listen.Tcp.Enabled = true;
                transportConfig.Listen.Tcp.InterfaceIp = "192.168.1.45";
                transportConfig.Listen.Tcp.Port = 4000;
                // my Ip Address  192.168.1.45
                // yash 192.168.1.32
                // 185.1.5.5:12345
                // Connect explicitly to a remote device on 
                transportConfig.Connect.TcpServers.Add("135.1.5.5:12345");
                // you can add as many TcpServers as you would like.

                //transportConfig.Connect.TcpServers.Add("192.168.1.45:4000");

                ditto.TransportConfig = transportConfig;

                ditto.StartSync();
            }
            catch (DittoException ex)
            {
                Console.WriteLine("There was an error starting Ditto.");
                Console.WriteLine("Here's the following error");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("Ditto cannot start sync but don't worry.");
                Console.WriteLine("Ditto will still work as a local database.");
            }

            Console.WriteLine("Welcome to Ditto's Task App");

            subscription = ditto.Store["tasks"].Find("!isDeleted").Subscribe();

            liveQuery = ditto.Store["tasks"].Find("!isDeleted").ObserveLocal((docs, _event) => {
                tasks = docs.ConvertAll(document => new Task(document));
            });

            ditto.Store["tasks"].Find("isDeleted == true").Evict();

            ListCommands();

            while (!isAskedToExit)
            {

                Console.Write("\nYour command: ");
                string command = Console.ReadLine();

                switch (command)
                {

                    case string s when command.StartsWith("--insert"):
                        string taskBody = s.Replace("--insert ", "");
                        ditto.Store["tasks"].Upsert(new Task(taskBody, false).ToDictionary());
                        break;
                    case string s when command.StartsWith("--toggle"):
                        string _idToToggle = s.Replace("--toggle ", "");
                        ditto.Store["tasks"]
                            .FindById(new DittoDocumentId(_idToToggle))
                            .Update((mutableDoc) => {
                                if (mutableDoc == null) return;
                                mutableDoc["isCompleted"].Set(!mutableDoc["isCompleted"].BooleanValue);
                            });
                        break;
                    case string s when command.StartsWith("--delete"):
                        string _idToDelete = s.Replace("--delete ", "");
                        ditto.Store["tasks"]
                            .FindById(new DittoDocumentId(_idToDelete))
                            .Update((mutableDoc) => {
                                if (mutableDoc == null) return;
                                mutableDoc["isDeleted"].Set(true);
                            });
                        break;
                    case { } when command.StartsWith("--list"):
                        tasks.ForEach(task =>
                        {
                            Console.WriteLine(task.ToString());
                        });
                        break;
                    case { } when command.StartsWith("--exit"):
                        Console.WriteLine("Good bye!");
                        isAskedToExit = true;
                        break;
                    default:
                        Console.WriteLine("Unknown command");
                        ListCommands();
                        break;
                }
            }
        }

        public static void ListCommands()
        {
            Console.WriteLine("************* Commands *************");
            Console.WriteLine("--insert my new task");
            Console.WriteLine("   Inserts a task");
            Console.WriteLine("   Example: \"--insert Get Milk\"");
            Console.WriteLine("--toggle myTaskTd");
            Console.WriteLine("   Toggles the isComplete property to the opposite value");
            Console.WriteLine("   Example: \"--toggle 1234abc\"");
            Console.WriteLine("--delete myTaskTd");
            Console.WriteLine("   Deletes a task");
            Console.WriteLine("   Example: \"--delete 1234abc\"");
            Console.WriteLine("--list");
            Console.WriteLine("   List the current tasks");
            Console.WriteLine("--exit");
            Console.WriteLine("   Exits the program");
            Console.WriteLine("************* Commands *************");
        }
    }
}