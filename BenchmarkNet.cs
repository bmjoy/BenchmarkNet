/*
 *  BenchmarkNet is a console application for testing the reliable UDP networking libraries
 *  Copyright (c) 2018 Stanislav Denisov
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
// ENet (https://github.com/lsalzman/enet) C# Wrapper (https://github.com/NateShoffner/ENetSharp)
using ENet;
// UNet (https://docs.unity3d.com/Manual/UNetUsingTransport.html)
using UnetServerDll;
// LiteNetLib (https://github.com/RevenantX/LiteNetLib)
using LiteNetLib;
using LiteNetLib.Utils;
// Lidgren (https://github.com/lidgren/lidgren-network-gen3)
using Lidgren.Network;
// MiniUDP (https://github.com/ashoulson/MiniUDP)
using MiniUDP;

namespace BenchmarkNet {
	public class BenchmarkNet {
		protected const string title = "BenchmarkNet";
		protected const string version = "1.01";
		protected const string ip = "127.0.0.1";
		protected static ushort port = 0;
		protected static ushort maxClients = 0;
		protected static int serverTickRate = 0;
		protected static int clientTickRate = 0;
		protected static int sendRate = 0;
		protected static int reliableMessages = 0;
		protected static int unreliableMessages = 0;
		protected static string message = "";
		protected static byte[] messageData;
		protected static bool processActive = false;
		protected static bool processCompleted = false;
		protected static bool processOverload = false;
		protected static bool maxClientsPass = true;
		protected static Thread serverThread;
		protected static volatile int clientsStartedCount = 0;
		protected static volatile int clientsConnectedCount = 0;
		protected static volatile int clientsDisconnectedCount = 0;
		protected static volatile int serverReliableSent = 0;
		protected static volatile int serverReliableReceived = 0;
		protected static volatile int serverReliableBytesSent = 0;
		protected static volatile int serverReliableBytesReceived = 0;
		protected static volatile int serverUnreliableSent = 0;
		protected static volatile int serverUnreliableReceived = 0;
		protected static volatile int serverUnreliableBytesSent = 0;
		protected static volatile int serverUnreliableBytesReceived = 0;
		protected static volatile int clientsReliableSent = 0;
		protected static volatile int clientsReliableReceived = 0;
		protected static volatile int clientsReliableBytesSent = 0;
		protected static volatile int clientsReliableBytesReceived = 0;
		protected static volatile int clientsUnreliableSent = 0;
		protected static volatile int clientsUnreliableReceived = 0;
		protected static volatile int clientsUnreliableBytesSent = 0;
		protected static volatile int clientsUnreliableBytesReceived = 0;
		private static ushort maxPeers = 0;
		private static byte selectedNetworkingLibrary = 0;
		private static readonly string[] networkingLibraries = new string[] {
			"ENet",
			"UNet",
			"LiteNetLib",
			"Lidgren",
			"MiniUDP"
		};

		private static void Main() {
			Console.Title = title;
			Console.SetIn(new StreamReader(Console.OpenStandardInput(8192), Console.InputEncoding, false, bufferSize: 1024));
			Console.WriteLine("Welcome to " + title + "!");
			Console.WriteLine("Version " + version);
			Console.WriteLine(Environment.NewLine + "Source code is available on GitHub (https://github.com/nxrighthere/BenchmarkNet)");
			Console.WriteLine("If you have any questions, contact me (nxrighthere@gmail.com)");
			Console.WriteLine(Environment.NewLine + "Select a networking library");

			for (int i = 0; i < networkingLibraries.Length; i++) {
				Console.WriteLine("(" + i + ") " + networkingLibraries[i]);
			}

			Console.Write(Environment.NewLine + "Enter the number (default 0): ");
			Byte.TryParse(Console.ReadLine(), out selectedNetworkingLibrary);

			ushort defaultPort = 9500;

			Console.Write("Port (default " + defaultPort + "): ");
			UInt16.TryParse(Console.ReadLine(), out port);
			
			if (port == 0)
				port = defaultPort;

			ushort defaultMaxClients = 1000;

			Console.Write("Simulated clients (default " + defaultMaxClients + "): ");
			UInt16.TryParse(Console.ReadLine(), out maxClients);

			if (maxClients == 0)
				maxClients = defaultMaxClients;

			int defaultServerTickRate = 64;

			Console.Write("Server tick rate (default " + defaultServerTickRate + "): ");
			Int32.TryParse(Console.ReadLine(), out serverTickRate);

			if (serverTickRate == 0)
				serverTickRate = defaultServerTickRate;

			int defaultClientTickRate = 64;

			Console.Write("Client tick rate (default " + defaultClientTickRate + "): ");
			Int32.TryParse(Console.ReadLine(), out clientTickRate);

			if (clientTickRate == 0)
				clientTickRate = defaultClientTickRate;

			int defaultSendRate = 15;

			Console.Write("Client send rate (default " + defaultSendRate + "): ");
			Int32.TryParse(Console.ReadLine(), out sendRate);

			if (sendRate == 0)
				sendRate = defaultSendRate;

			int defaultReliableMessages = 500;

			Console.Write("Reliable messages per client (default " + defaultReliableMessages + "): ");
			Int32.TryParse(Console.ReadLine(), out reliableMessages);

			if (reliableMessages == 0)
				reliableMessages = defaultReliableMessages;

			int defaultUnreliableMessages = 1000;

			Console.Write("Unreliable messages per client (default " + defaultUnreliableMessages + "): ");
			Int32.TryParse(Console.ReadLine(), out unreliableMessages);

			if (unreliableMessages == 0)
				unreliableMessages = defaultUnreliableMessages;

			string defaultMessage = "Sometimes we just need a good networking library";

			Console.Write("Message (default " + defaultMessage.Length + " characters): ");
			message = Console.ReadLine();

			if (message == string.Empty)
				message = defaultMessage;

			messageData = Encoding.ASCII.GetBytes(message);

			Console.CursorVisible = false;
			Console.Clear();
			
			processActive = true;

			if (selectedNetworkingLibrary == 0)
				Library.Initialize();

			maxPeers = ushort.MaxValue;
			maxClientsPass = (selectedNetworkingLibrary > 0 ? maxClients <= maxPeers : maxClients <= Native.ENET_PROTOCOL_MAXIMUM_PEER_ID);

			if (!maxClientsPass)
				maxClients = Math.Min(Math.Max((ushort)1, (ushort)maxClients), (selectedNetworkingLibrary > 0 ? maxPeers : (ushort)Native.ENET_PROTOCOL_MAXIMUM_PEER_ID));

			if (selectedNetworkingLibrary == 0)
				serverThread = new Thread(ENetBenchmark.Server);
			else if (selectedNetworkingLibrary == 1)
				serverThread = new Thread(UNetBenchmark.Server);
			else if (selectedNetworkingLibrary == 2)
				serverThread = new Thread(LiteNetLibBenchmark.Server);
			else if (selectedNetworkingLibrary == 3)
				serverThread = new Thread(LidgrenBenchmark.Server);
			else
				serverThread = new Thread(MiniUDPBenchmark.Server);

			serverThread.Start();
			Thread.Sleep(100);
			
			Task infoTask = Info();
			Task superviseTask = Supervise();
			Task spawnTask = Spawn();

			Console.ReadKey();
			processActive = false;
			Environment.Exit(0);
		}

		private static async Task Info() {
			await Task.Factory.StartNew(() => {
				int spinnerTimer = 0;
				int spinnerSequence = 0;
				string spinner = "";
				Stopwatch stopwatch = new Stopwatch();

				stopwatch.Start();

				while (processActive) {
					Console.CursorVisible = false;
					Console.SetCursorPosition(0, 0);
					Console.WriteLine("Benchmarking " + networkingLibraries[selectedNetworkingLibrary] + "...");
					Console.WriteLine("Server tick rate: " + serverTickRate + ", Client tick rate: " + clientTickRate + " (ticks per second)");
					Console.WriteLine(maxClients + " clients, " + reliableMessages + " reliable and " + unreliableMessages + " unreliable messages per client, " + messageData.Length + " bytes per message, " + sendRate + " messages per second");
					
					if (!maxClientsPass) {
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine("This networking library doesn't support more than " + (selectedNetworkingLibrary > 0 ? maxPeers : Native.ENET_PROTOCOL_MAXIMUM_PEER_ID) + " peers per server!");
						Console.ResetColor();
					}

					Console.WriteLine(Environment.NewLine + "Server status: " + (processOverload ? "Overload" : (processCompleted ? "Completed" : (serverThread.IsAlive ? "Running" : "Failure"))));
					Console.WriteLine("Clients status: " + clientsStartedCount + " started, " + clientsConnectedCount + " connected, " + clientsDisconnectedCount + " dropped");
					Console.WriteLine("Clients sent -> Reliable: " + clientsReliableSent + " messages (" + clientsReliableBytesSent + " bytes), Unreliable: " + clientsUnreliableSent + " messages (" + clientsUnreliableBytesSent + " bytes)");
					Console.WriteLine("Server received <- Reliable: " + serverReliableReceived + " messages (" + serverReliableBytesReceived + " bytes), Unreliable: " + serverUnreliableReceived + " messages (" + serverUnreliableBytesReceived + " bytes)");
					Console.WriteLine("Server sent -> Reliable: " + serverReliableSent + " messages (" + serverReliableBytesSent + " bytes), Unreliable: " + serverUnreliableSent + " messages (" + serverUnreliableBytesSent + " bytes)");
					Console.WriteLine("Clients received <- Reliable: " + clientsReliableReceived + " messages (" + clientsReliableBytesReceived + " bytes), Unreliable: " + clientsUnreliableReceived + " messages (" + clientsUnreliableBytesReceived + " bytes)");
					Console.WriteLine("Total - Reliable: " + (clientsReliableSent + serverReliableReceived + serverReliableSent + clientsReliableReceived) + " messages (" + (clientsReliableBytesSent + serverReliableBytesReceived + serverReliableBytesSent + clientsReliableBytesReceived) + " bytes), Unreliable: " + (clientsUnreliableSent + serverUnreliableReceived + serverUnreliableSent + clientsUnreliableReceived) + " messages (" + (clientsUnreliableBytesSent + serverUnreliableBytesReceived + serverUnreliableBytesSent + clientsUnreliableBytesReceived) + " bytes)");
					Console.WriteLine("Expected - Reliable: " + (maxClients * reliableMessages * 4) + " messages (" + (maxClients * reliableMessages * messageData.Length * 4) + " bytes), Unreliable: " + (maxClients * unreliableMessages * 4) + " messages (" + (maxClients * unreliableMessages * messageData.Length * 4) + " bytes)");
					Console.WriteLine("Elapsed time: " + stopwatch.Elapsed.Hours.ToString("00") + ":" + stopwatch.Elapsed.Minutes.ToString("00") + ":" + stopwatch.Elapsed.Seconds.ToString("00"));

					if (spinnerTimer >= 10) {
						spinnerSequence++;
						spinnerTimer = 0;
					} else {
						spinnerTimer++;
					}

					switch (spinnerSequence % 4) {
						case 0: spinner = "/";
							break;
						case 1: spinner = "—";
							break;
						case 2: spinner = "\\";
							break;
						case 3: spinner = "|";
							break;
					}

					Console.WriteLine(Environment.NewLine + "Press any key to stop the process " + spinner);
					Thread.Sleep(15);
				}

				if (!processActive && processCompleted) {
					Console.SetCursorPosition(0, Console.CursorTop - 1);
					Console.WriteLine("Process completed! Press any key to exit...");
				}
				
				stopwatch.Stop();
			}, TaskCreationOptions.LongRunning);
		}

		private static async Task Supervise() {
			await Task.Factory.StartNew(() => {
				decimal lastData = 0;

				while (processActive) {
					Thread.Sleep(1000);

					decimal currentData = (decimal)(serverReliableSent + serverReliableReceived + serverUnreliableSent + serverUnreliableReceived + clientsReliableSent + clientsReliableReceived + clientsUnreliableSent + clientsUnreliableReceived);

					if (currentData == lastData) {
						if (currentData != 0 && ((currentData / (decimal)(maxClients * (reliableMessages + unreliableMessages) * 4)) * 100) < 90)
							processOverload = true;

						processCompleted = true;
						Thread.Sleep(100);
						processActive = false;

						break;
					}
					
					lastData = currentData;
				}

				if (!processActive) {
					if (selectedNetworkingLibrary == 0)
						Library.Deinitialize();
				}
			}, TaskCreationOptions.LongRunning);
		}

		private static async Task Spawn() {
			await Task.Factory.StartNew(() => {
				List<Task> clients = new List<Task>();

				for (int i = 0; i < maxClients; i++) {
					if (!processActive)
						break;

					if (selectedNetworkingLibrary == 0)
						clients.Add(ENetBenchmark.Client());
					else if (selectedNetworkingLibrary == 1)
						clients.Add(UNetBenchmark.Client());
					else if (selectedNetworkingLibrary == 2)
						clients.Add(LiteNetLibBenchmark.Client());
					else if (selectedNetworkingLibrary == 3)
						clients.Add(LidgrenBenchmark.Client());
					else
						clients.Add(MiniUDPBenchmark.Client());
					
					Interlocked.Increment(ref clientsStartedCount);
					Thread.Sleep(15);
				}
			}, TaskCreationOptions.LongRunning);
		}
	}

	public class ENetBenchmark : BenchmarkNet {
		private static void SendReliable(byte[] data, byte channelID, Peer peer) {
			Packet packet = new Packet();
			packet.Create(data, 0, data.Length, PacketFlags.Reliable); // Reliable Sequenced
			peer.Send(channelID, packet);
		}

		private static void SendUnreliable(byte[] data, byte channelID, Peer peer) {
			Packet packet = new Packet();
			packet.Create(data, 0, data.Length, PacketFlags.None); // Unreliable Sequenced
			peer.Send(channelID, packet);
		}

		public static void Server() {
			using (Host server = new Host()) {
				server.Create(port, maxClients);
				
				Event serverEvent = new Event();

				while (processActive) {
					server.Service(1000 / serverTickRate, out serverEvent);

					switch (serverEvent.Type) {
						case EventType.Receive:
							byte[] data = serverEvent.Packet.GetBytes();

							if (serverEvent.ChannelID == 2) {
								Interlocked.Increment(ref serverReliableReceived);
								SendReliable(messageData, 0, serverEvent.Peer);
								Interlocked.Increment(ref serverReliableSent);
								Interlocked.Add(ref serverReliableBytesSent, messageData.Length);
								Interlocked.Add(ref serverReliableBytesReceived, data.Length);
							} else if (serverEvent.ChannelID == 3) {
								Interlocked.Increment(ref serverUnreliableReceived);
								SendUnreliable(messageData, 1, serverEvent.Peer);
								Interlocked.Increment(ref serverUnreliableSent);
								Interlocked.Add(ref serverUnreliableBytesSent, messageData.Length);
								Interlocked.Add(ref serverUnreliableBytesReceived, data.Length);
							}

							serverEvent.Packet.Dispose();

							break;
					}
				}
			}
		}

		public static async Task Client() {
			await Task.Factory.StartNew(() => {
				Host client = new Host();
				
				client.Create(null, 1);

				Address address = new Address();

				address.SetHost(ip);
				address.Port = port;

				Peer peer = client.Connect(address, 4, 0);
				Event clientEvent = new Event();
				
				int reliableToSend = 0;
				int unreliableToSend = 0;
				int reliableSentCount = 0;
				int unreliableSentCount = 0;

				Task.Factory.StartNew(async() => {
					while (processActive) {
						if (reliableToSend > 0) {
							SendReliable(messageData, 2, peer);
							Interlocked.Decrement(ref reliableToSend);
							Interlocked.Increment(ref reliableSentCount);
							Interlocked.Increment(ref clientsReliableSent);
							Interlocked.Add(ref clientsReliableBytesSent, messageData.Length);
						}

						if (unreliableToSend > 0) {
							SendUnreliable(messageData, 3, peer);
							Interlocked.Decrement(ref unreliableToSend);
							Interlocked.Increment(ref unreliableSentCount);
							Interlocked.Increment(ref clientsUnreliableSent);
							Interlocked.Add(ref clientsUnreliableBytesSent, messageData.Length);
						}

						await Task.Delay(1000 / sendRate);
					}
				}, TaskCreationOptions.LongRunning);

				while (processActive) {
					client.Service(1000 / clientTickRate, out clientEvent);

					switch (clientEvent.Type) {
						case EventType.Connect:
							Interlocked.Increment(ref clientsConnectedCount);
							Interlocked.Exchange(ref reliableToSend, reliableMessages);
							Interlocked.Exchange(ref unreliableToSend, unreliableMessages);

							break;

						case EventType.Disconnect:
							Interlocked.Increment(ref clientsDisconnectedCount);

							break;

						case EventType.Receive:
							byte[] data = clientEvent.Packet.GetBytes();

							if (clientEvent.ChannelID == 0) {
								Interlocked.Increment(ref clientsReliableReceived);
								Interlocked.Add(ref clientsReliableBytesReceived, data.Length);
							} else if (clientEvent.ChannelID == 1) {
								Interlocked.Increment(ref clientsUnreliableReceived);
								Interlocked.Add(ref clientsUnreliableBytesReceived, data.Length);
							}
							
							clientEvent.Packet.Dispose();

							break;
					}
				}

				peer.Disconnect(0);
			}, TaskCreationOptions.LongRunning);
		}
	}

	public class UNetBenchmark : BenchmarkNet {
		public static void Server() {
			GlobalConfig globalConfig = new GlobalConfig();

			globalConfig.ReactorMaximumSentMessages = 0;
			globalConfig.ReactorMaximumReceivedMessages = 0;

			ConnectionConfig connectionConfig = new ConnectionConfig();

			int reliableChannel = connectionConfig.AddChannel(QosType.ReliableSequenced);
			int unreliableChannel = connectionConfig.AddChannel(QosType.UnreliableSequenced);

			HostTopology topology = new HostTopology(connectionConfig, maxClients);
			
			topology.SentMessagePoolSize = ushort.MaxValue;
			topology.ReceivedMessagePoolSize = ushort.MaxValue;

			using (NetLibraryManager server = new NetLibraryManager(globalConfig)) {
				int host = server.AddHost(topology, port, ip);

				int hostID, connectionID, channelID, dataLength;
				byte[] buffer = new byte[1024];
				NetworkEventType netEvent;

				while (processActive) {
					byte error;

					while ((netEvent = server.Receive(out hostID, out connectionID, out channelID, buffer, buffer.Length, out dataLength, out error)) != NetworkEventType.Nothing) {
						switch (netEvent) {
							case NetworkEventType.DataEvent:
								if (channelID == 0) {
									Interlocked.Increment(ref serverReliableReceived);
									server.Send(hostID, connectionID, reliableChannel, messageData, messageData.Length, out error); 
									Interlocked.Increment(ref serverReliableSent);
									Interlocked.Add(ref serverReliableBytesSent, messageData.Length);
									Interlocked.Add(ref serverReliableBytesReceived, dataLength);
								} else if (channelID == 1) {
									Interlocked.Increment(ref serverUnreliableReceived);
									server.Send(hostID, connectionID, unreliableChannel, messageData, messageData.Length, out error); 
									Interlocked.Increment(ref serverUnreliableSent);
									Interlocked.Add(ref serverUnreliableBytesSent, messageData.Length);
									Interlocked.Add(ref serverUnreliableBytesReceived, dataLength);
								}

								break;
						}
					}

					Thread.Sleep(1000 / serverTickRate);
				}
			}
		}

		public static async Task Client() {
			await Task.Factory.StartNew(() => {
				ConnectionConfig connectionConfig = new ConnectionConfig();
				
				int reliableChannel = connectionConfig.AddChannel(QosType.ReliableSequenced);
				int unreliableChannel = connectionConfig.AddChannel(QosType.UnreliableSequenced);
				
				NetLibraryManager client = new NetLibraryManager();
				int host = client.AddHost(new HostTopology(connectionConfig, 1), 0, null);
				
				byte connectionError;
				int connection = client.Connect(host, ip, port, 0, out connectionError);

				int reliableToSend = 0;
				int unreliableToSend = 0;
				int reliableSentCount = 0;
				int unreliableSentCount = 0;

				Task.Factory.StartNew(async() => {
					while (processActive) {
						byte error;
						
						if (reliableToSend > 0) {
							client.Send(host, connection, reliableChannel, messageData, messageData.Length, out error); 
							Interlocked.Decrement(ref reliableToSend);
							Interlocked.Increment(ref reliableSentCount);
							Interlocked.Increment(ref clientsReliableSent);
							Interlocked.Add(ref clientsReliableBytesSent, messageData.Length);
						}

						if (unreliableToSend > 0) {
							client.Send(host, connection, unreliableChannel, messageData, messageData.Length, out error); 
							Interlocked.Decrement(ref unreliableToSend);
							Interlocked.Increment(ref unreliableSentCount);
							Interlocked.Increment(ref clientsUnreliableSent);
							Interlocked.Add(ref clientsUnreliableBytesSent, messageData.Length);
						}

						await Task.Delay(1000 / sendRate);
					}
				}, TaskCreationOptions.LongRunning);

				int hostID, connectionID, channelID, dataLength;
				byte[] buffer = new byte[1024];
				NetworkEventType netEvent;

				while (processActive) {
					byte error;

					while ((netEvent = client.Receive(out hostID, out connectionID, out channelID, buffer, buffer.Length, out dataLength, out error)) != NetworkEventType.Nothing) {
						switch (netEvent) {
							case NetworkEventType.ConnectEvent:
								Interlocked.Increment(ref clientsConnectedCount);
								Interlocked.Exchange(ref reliableToSend, reliableMessages);
								Interlocked.Exchange(ref unreliableToSend, unreliableMessages);

								break;

							case NetworkEventType.DisconnectEvent:
								Interlocked.Increment(ref clientsDisconnectedCount);

								break;

							case NetworkEventType.DataEvent:
								if (channelID == 0) {
									Interlocked.Increment(ref clientsReliableReceived);
									Interlocked.Add(ref clientsReliableBytesReceived, dataLength);
								} else if (channelID == 1) {
									Interlocked.Increment(ref clientsUnreliableReceived);
									Interlocked.Add(ref clientsUnreliableBytesReceived, dataLength);
								}

								break;
						}
					}

					Thread.Sleep(1000 / clientTickRate);
				}

				byte disconnectError;

				client.Disconnect(host, connection, out disconnectError);
			}, TaskCreationOptions.LongRunning);
		}
	}

	public class LiteNetLibBenchmark : BenchmarkNet {
		private static void SendReliable(byte[] data, LiteNetLib.NetPeer peer) {
			NetDataWriter writer = new NetDataWriter();
			writer.Put(data);
			peer.Send(writer, DeliveryMethod.ReliableOrdered); // Reliable Ordered (https://github.com/RevenantX/LiteNetLib/issues/68)
		}

		private static void SendUnreliable(byte[] data, LiteNetLib.NetPeer peer) {
			NetDataWriter writer = new NetDataWriter();
			writer.Put(data);
			peer.Send(writer, DeliveryMethod.Sequenced); // Unreliable Sequenced
		}

		public static void Server() {
			EventBasedNetListener listener = new EventBasedNetListener();
			NetManager server = new NetManager(listener, maxClients);
			
			server.MergeEnabled = true;
			server.Start(port);

			listener.ConnectionRequestEvent += (request) => {
				request.AcceptIfKey(title + "Key");
			};

			listener.NetworkReceiveEvent += (peer, reader, deliveryMethod) => {
				byte[] data = reader.Data;

				if (deliveryMethod == DeliveryMethod.ReliableOrdered) {
					Interlocked.Increment(ref serverReliableReceived);
					SendReliable(messageData, peer);
					Interlocked.Increment(ref serverReliableSent);
					Interlocked.Add(ref serverReliableBytesSent, messageData.Length);
					Interlocked.Add(ref serverReliableBytesReceived, data.Length);
				} else if (deliveryMethod == DeliveryMethod.Sequenced) {
					Interlocked.Increment(ref serverUnreliableReceived);
					SendUnreliable(messageData, peer);
					Interlocked.Increment(ref serverUnreliableSent);
					Interlocked.Add(ref serverUnreliableBytesSent, messageData.Length);
					Interlocked.Add(ref serverUnreliableBytesReceived, data.Length);
				}
			};

			while (processActive) {
				server.PollEvents();
				Thread.Sleep(1000 / serverTickRate);
			}

			server.Stop();
		}

		public static async Task Client() {
			await Task.Factory.StartNew(() => {
				EventBasedNetListener listener = new EventBasedNetListener();
				NetManager client = new NetManager(listener);
				
				client.MergeEnabled = true;
				client.Start();
				client.Connect(ip, port, title + "Key");

				int reliableToSend = 0;
				int unreliableToSend = 0;
				int reliableSentCount = 0;
				int unreliableSentCount = 0;

				Task.Factory.StartNew(async() => {
					while (processActive) {
						if (reliableToSend > 0) {
							SendReliable(messageData, client.GetFirstPeer());
							Interlocked.Decrement(ref reliableToSend);
							Interlocked.Increment(ref reliableSentCount);
							Interlocked.Increment(ref clientsReliableSent);
							Interlocked.Add(ref clientsReliableBytesSent, messageData.Length);
						}

						if (unreliableToSend > 0) {
							SendUnreliable(messageData, client.GetFirstPeer());
							Interlocked.Decrement(ref unreliableToSend);
							Interlocked.Increment(ref unreliableSentCount);
							Interlocked.Increment(ref clientsUnreliableSent);
							Interlocked.Add(ref clientsUnreliableBytesSent, messageData.Length);
						}

						await Task.Delay(1000 / sendRate);
					}
				}, TaskCreationOptions.LongRunning);

				listener.PeerConnectedEvent += (peer) => {
					Interlocked.Increment(ref clientsConnectedCount);
					Interlocked.Exchange(ref reliableToSend, reliableMessages);
					Interlocked.Exchange(ref unreliableToSend, unreliableMessages);
				};

				listener.PeerDisconnectedEvent += (peer, info) => {
					Interlocked.Increment(ref clientsDisconnectedCount);
				};

				listener.NetworkReceiveEvent += (peer, reader, deliveryMethod) => {
					byte[] data = reader.Data;

					if (deliveryMethod == DeliveryMethod.ReliableOrdered) {
						Interlocked.Increment(ref clientsReliableReceived);
						Interlocked.Add(ref clientsReliableBytesReceived, data.Length);
					} else if (deliveryMethod == DeliveryMethod.Sequenced) {
						Interlocked.Increment(ref clientsUnreliableReceived);
						Interlocked.Add(ref clientsUnreliableBytesReceived, data.Length);
					}
				};

				while (processActive) {
					client.PollEvents();
					Thread.Sleep(1000 / clientTickRate);
				}

				client.Stop();
			}, TaskCreationOptions.LongRunning);
		}
	}

	public class LidgrenBenchmark : BenchmarkNet {
		private static void SendReliable(byte[] data, NetConnection connection, NetOutgoingMessage netMessage, int channelID) {
			netMessage.Write(data);
			connection.SendMessage(netMessage, NetDeliveryMethod.ReliableSequenced, channelID);
		}

		private static void SendUnreliable(byte[] data, NetConnection connection, NetOutgoingMessage netMessage, int channelID) {
			netMessage.Write(data);
			connection.SendMessage(netMessage, NetDeliveryMethod.UnreliableSequenced, channelID);
		}

		public static void Server() {
			NetPeerConfiguration config = new NetPeerConfiguration(title + "Config");
			
			config.Port = port;
			config.MaximumConnections = maxClients;

			NetServer server = new NetServer(config);
			server.Start();

			NetIncomingMessage netMessage;

			while (processActive) {
				while ((netMessage = server.ReadMessage()) != null) {
					switch (netMessage.MessageType) {
						case NetIncomingMessageType.Data:
							byte[] data = netMessage.ReadBytes(netMessage.LengthBytes);

							if (netMessage.SequenceChannel == 2) {
								Interlocked.Increment(ref serverReliableReceived);
								SendReliable(messageData, netMessage.SenderConnection, server.CreateMessage(), 0);
								Interlocked.Increment(ref serverReliableSent);
								Interlocked.Add(ref serverReliableBytesSent, messageData.Length);
								Interlocked.Add(ref serverReliableBytesReceived, data.Length);
							} else if (netMessage.SequenceChannel == 3) {
								Interlocked.Increment(ref serverUnreliableReceived);
								SendUnreliable(messageData, netMessage.SenderConnection, server.CreateMessage(), 1);
								Interlocked.Increment(ref serverUnreliableSent);
								Interlocked.Add(ref serverUnreliableBytesSent, messageData.Length);
								Interlocked.Add(ref serverUnreliableBytesReceived, data.Length);
							}

							break;
					}

					server.Recycle(netMessage);
				}
				
				Thread.Sleep(1000 / serverTickRate);
			}

			server.Shutdown(title + "Shutdown");
		}

		public static async Task Client() {
			await Task.Factory.StartNew(() => {
				NetPeerConfiguration config = new NetPeerConfiguration(title + "Config");
				NetClient client = new NetClient(config);
				
				client.Start();
				client.Connect(ip, port);

				int reliableToSend = 0;
				int unreliableToSend = 0;
				int reliableSentCount = 0;
				int unreliableSentCount = 0;

				Task.Factory.StartNew(async() => {
					while (processActive) {
						if (reliableToSend > 0) {
							SendReliable(messageData, client.ServerConnection, client.CreateMessage(), 2);
							Interlocked.Decrement(ref reliableToSend);
							Interlocked.Increment(ref reliableSentCount);
							Interlocked.Increment(ref clientsReliableSent);
							Interlocked.Add(ref clientsReliableBytesSent, messageData.Length);
						}

						if (unreliableToSend > 0) {
							SendUnreliable(messageData, client.ServerConnection, client.CreateMessage(), 3);
							Interlocked.Decrement(ref unreliableToSend);
							Interlocked.Increment(ref unreliableSentCount);
							Interlocked.Increment(ref clientsUnreliableSent);
							Interlocked.Add(ref clientsUnreliableBytesSent, messageData.Length);
						}

						await Task.Delay(1000 / sendRate);
					}
				}, TaskCreationOptions.LongRunning);

				NetIncomingMessage netMessage;

				while (processActive) {
					while ((netMessage = client.ReadMessage()) != null) {
						switch (netMessage.MessageType) {
							case NetIncomingMessageType.StatusChanged:
								NetConnectionStatus status = (NetConnectionStatus)netMessage.ReadByte();

								if (status == NetConnectionStatus.Connected) {
									Interlocked.Increment(ref clientsConnectedCount);
									Interlocked.Exchange(ref reliableToSend, reliableMessages);
									Interlocked.Exchange(ref unreliableToSend, unreliableMessages);
								} else if (status == NetConnectionStatus.Disconnected) {
									Interlocked.Increment(ref clientsDisconnectedCount);
								}

								break;

							case NetIncomingMessageType.Data:
								byte[] data = netMessage.ReadBytes(netMessage.LengthBytes);

								if (netMessage.SequenceChannel == 0) {
									Interlocked.Increment(ref clientsReliableReceived);
									Interlocked.Add(ref clientsReliableBytesReceived, data.Length);
								} else if (netMessage.SequenceChannel == 1) {
									Interlocked.Increment(ref clientsUnreliableReceived);
									Interlocked.Add(ref clientsUnreliableBytesReceived, data.Length);
								}

								break;
						}

						client.Recycle(netMessage);
					}
					
					Thread.Sleep(1000 / clientTickRate);
				}

				client.Shutdown(title + "Shutdown");
			}, TaskCreationOptions.LongRunning);
		}
	}

	public class MiniUDPBenchmark : BenchmarkNet {
		private static void SendReliable(byte[] data, MiniUDP.NetPeer peer) {
			peer.QueueNotification(data, (ushort)data.Length); // Reliable Ordered (https://github.com/ashoulson/MiniUDP/blob/master/MiniUDP/Threaded/NetPeer.cs#L105)
		}

		private static void SendUnreliable(byte[] data, MiniUDP.NetPeer peer) {
			peer.SendPayload(data, (ushort)data.Length); // Unreliable Sequenced
		}

		public static void Server() {
			NetCore server = new NetCore(title, true);

			server.Host(port);
			
			server.PeerNotification += (peer, data, dataLength) => {
				Interlocked.Increment(ref serverReliableReceived);
				SendReliable(messageData, peer);
				Interlocked.Increment(ref serverReliableSent);
				Interlocked.Add(ref serverReliableBytesSent, messageData.Length);
				Interlocked.Add(ref serverReliableBytesReceived, dataLength);
			};

			server.PeerPayload += (peer, data, dataLength) => {
				Interlocked.Increment(ref serverUnreliableReceived);
				SendUnreliable(messageData, peer);
				Interlocked.Increment(ref serverUnreliableSent);
				Interlocked.Add(ref serverUnreliableBytesSent, messageData.Length);
				Interlocked.Add(ref serverUnreliableBytesReceived, dataLength);
			};

			while (processActive) {
				server.PollEvents();
				Thread.Sleep(1000 / serverTickRate);
			}

			server.Stop();
		}

		public static async Task Client() {
			await Task.Factory.StartNew(() => {
				NetCore client = new NetCore(title, false);
				
				MiniUDP.NetPeer connection = client.Connect(NetUtil.StringToEndPoint(ip + ":" + port), "");

				int reliableToSend = 0;
				int unreliableToSend = 0;
				int reliableSentCount = 0;
				int unreliableSentCount = 0;

				Task.Factory.StartNew(async() => {
					while (processActive) {
						if (reliableToSend > 0) {
							SendReliable(messageData, connection);
							Interlocked.Decrement(ref reliableToSend);
							Interlocked.Increment(ref reliableSentCount);
							Interlocked.Increment(ref clientsReliableSent);
							Interlocked.Add(ref clientsReliableBytesSent, messageData.Length);
						}

						if (unreliableToSend > 0) {
							SendUnreliable(messageData, connection);
							Interlocked.Decrement(ref unreliableToSend);
							Interlocked.Increment(ref unreliableSentCount);
							Interlocked.Increment(ref clientsUnreliableSent);
							Interlocked.Add(ref clientsUnreliableBytesSent, messageData.Length);
						}

						await Task.Delay(1000 / sendRate);
					}
				}, TaskCreationOptions.LongRunning);

				client.PeerConnected += (peer, token) => {
					Interlocked.Increment(ref clientsConnectedCount);
					Interlocked.Exchange(ref reliableToSend, reliableMessages);
					Interlocked.Exchange(ref unreliableToSend, unreliableMessages);
				};

				client.PeerClosed += (peer, reason, kickReason, error) => {
					Interlocked.Increment(ref clientsDisconnectedCount);
				};

				client.PeerNotification += (peer, data, dataLength) => {
					Interlocked.Increment(ref clientsReliableReceived);
					Interlocked.Add(ref clientsReliableBytesReceived, dataLength);
				};

				client.PeerPayload += (peer, data, dataLength) => {
					Interlocked.Increment(ref clientsUnreliableReceived);
					Interlocked.Add(ref clientsUnreliableBytesReceived, dataLength);
				};

				while (processActive) {
					client.PollEvents();
					Thread.Sleep(1000 / clientTickRate);
				}

				client.Stop();
			}, TaskCreationOptions.LongRunning);
		}
	}
}
