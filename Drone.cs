using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using static KeyboardHook;

namespace SCUMBot
{
    static class Players
    {
        public const string CurrentAdmin = Bot;

        public const string Bot = "STEAMID64_OF_BOT_CLIENT";
    }

    public partial class Drone : Form
    {
        System.Timers.Timer timer = new System.Timers.Timer();
        System.Timers.Timer timer2 = new System.Timers.Timer();

        bool DoingDelivery = false;

        private string identToken;
        public string connectionString = "SERVER=host;DATABASE=scumbot_development;UID=username;PASSWORD=password;";

        public Drone()
        {
            try
            {
                DebugLog($"Starting SCUM Bot");

                InitializeComponent();

                // Register Drone.
                Identify();

                // Teleport to the safe zone.
                DebugLog("Teleported to safezone");
                ScumManager.Teleport(Players.CurrentAdmin, "-288543.594", "320024.781", "86025.195");
                Thread.Sleep(10000);

                timer.Interval = 10000; // 10 seconds
                timer.Elapsed += ProcessDeliveries;
                timer.Start();

                timer2.Interval = 300000; // 5 minutes
                timer2.Elapsed += CaptureSquads;
                timer2.Start();
            }
            catch (Exception ex)
            {
                DebugLog($"Exception: {ex}");
            }
        }

        private void DebugLog(string content)
        {
            Logger.LogWrite(content);
            Console.WriteLine("{0} {1}: {2}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString(), content);
        }

        private void Identify()
        {
            string m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (!File.Exists($"{m_exePath}\\identity.drone"))
            {
                identToken = Guid.NewGuid().ToString();
                File.WriteAllText($"{m_exePath}\\identity.drone", identToken);
                DebugLog($"Created a new identity file and token.");
            }

            // First is usually a bad idea becauase it will crash if the file doesn't exist with a line.
            // Since we are making the file above should be fine to use this, if no file probably want a boom for now.
            identToken = File.ReadLines($"{m_exePath}\\identity.drone").First();
            
            Register(identToken);
        }

        private void Register(string identToken)
        {
            var connection = new MySqlConnection(connectionString);
            var cmd = connection.CreateCommand();
            cmd.CommandText = $"INSERT INTO drones (name, state, token) SELECT '{Environment.MachineName}', 'active', '{identToken}' WHERE NOT EXISTS (SELECT * FROM drones WHERE token = '{identToken}' LIMIT 1);";

            try
            {
                connection.Open();
                var reader = cmd.ExecuteNonQuery();
                DebugLog("Drone registration updated with flight control");
            }
            catch (Exception ex)
            {
                DebugLog(ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        private bool FlightApproved()
        {
            var connection = new MySqlConnection(connectionString);
            var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT state FROM drones WHERE token = '{identToken}';";
            
            try
            {
                connection.Open();
                var reader = cmd.ExecuteReader();

                if (!reader.HasRows)
                {
                    DebugLog("Drone not registered, cannot find a valid flight path");
                    return false;
                }

                reader.Read();
                return reader.GetString("state") == "active";
            }
            catch(Exception ex)
            {
                DebugLog(ex.Message);
                return false;
            }
            finally
            {
                connection.Close();
            }
        }

        private bool BotOnline()
        {
            var connection = new MySqlConnection(connectionString);
            var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT presence FROM users WHERE steamId64 = '{Players.CurrentAdmin}';";

            try
            {
                connection.Open();
                var reader = cmd.ExecuteReader();

                if (!reader.HasRows)
                {
                    DebugLog("Cant find the bot in the database.");
                    return false;
                }

                reader.Read();
                return reader.GetString("presence") == "online";
            }
            catch (Exception ex)
            {
                DebugLog(ex.Message);
                return false;
            }
            finally
            {
                connection.Close();
            }
        }

        private void TransponderPing()
        {
            var connection = new MySqlConnection(connectionString);
            var cmd = connection.CreateCommand();
            cmd.CommandText = $"UPDATE drones SET lastSeenAt = NOW() WHERE token = '{identToken}';";

            try
            {
                connection.Open();
                var reader = cmd.ExecuteNonQuery();
                DebugLog("Drone transponder ping completed");
            }
            catch (Exception ex)
            {
                DebugLog(ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        private void Announcements()
        {
            var connection = new MySqlConnection(connectionString);
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM announcements WHERE publishedAt IS NULL ORDER BY createdAt LIMIT 1;";

            try
            {
                connection.Open();
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var content = reader.GetString("content");
                    ScumManager.Announce(content);
                    MarkAnnouncementPublished(reader.GetInt32("id"));
                }
            }
            catch (Exception ex)
            {
                DebugLog(ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        private void MarkAnnouncementPublished(int announcementId)
        {
            var connection = new MySqlConnection(connectionString);
            var cmd = connection.CreateCommand();
            cmd.CommandText = $"UPDATE announcements SET publishedAt = NOW() WHERE id = {announcementId};";

            try
            {
                connection.Open();
                var reader = cmd.ExecuteNonQuery();
                DebugLog($"Marked announcement (#{announcementId}) as published");
            }
            catch (Exception ex)
            {
                DebugLog(ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        private void CaptureSquads(object sender, EventArgs e)
        {
            if (DoingDelivery) return;

            // Prevent deliveries from running while we are interacting with the game and clipboard.
            DoingDelivery = true;

            string m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string clipboardContent = null;
            Exception threadEx = null;
            Thread staThread = new Thread(
                delegate ()
                {
                    try
                    {
                        ScumManager.DumpAllSquadsInfoList();
                        Thread.Sleep(2000);

                        clipboardContent = Clipboard.GetText();
                    }

                    catch (Exception ex)
                    {
                        threadEx = ex;
                    }
                });
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();

            if (clipboardContent.Contains("[Squad"))
            {
                File.WriteAllText($"{m_exePath}\\squads.txt", clipboardContent);
                UploadSquads();
            }

            DoingDelivery = false;
        }

        private void UploadSquads()
        {
            string m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string filenameTimestamp = DateTime.UtcNow.ToString("yyyyMMdd");

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"ftp://ftphost:28321/SCUM/Saved/SaveFiles/Logs/squads_{filenameTimestamp}.log");
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential("username", "password");
            request.UsePassive = true;
            
            byte[] fileContents;
            using (StreamReader sourceStream = new StreamReader($"{m_exePath}\\squads.txt"))
            {
                fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
            }

            request.ContentLength = fileContents.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(fileContents, 0, fileContents.Length);
            }

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                DebugLog($"Upload File Complete, status {response.StatusDescription}");
            }
        }

        private void ProcessDeliveries(object sender, EventArgs e)
        {
            TransponderPing();
            Announcements();

            if (!BotOnline())
            {
                DoingDelivery = false;
                DebugLog("The bot isn't online bro.");
                return;
            }
            else
            {
                DebugLog("The bot is online and will deliver.");
            }

            if (!FlightApproved())
            {
                DoingDelivery = false;
                DebugLog("Flight status grounded.");
                return;
            }
            else
            {
                DebugLog("Flight status active");
            }

            DebugLog($"Processing Deliveries...");

            if (DoingDelivery == true)
            {
                DebugLog("Already doing a delivery, skipping");
                return; // Do nothing if already doing a delivery.
            }
            else
            {
                var connection = new MySqlConnection(connectionString);
                var ordersConnection = new MySqlConnection(connectionString);
                var giveItemsConnection = new MySqlConnection(connectionString);
                var connection_updatedelivery = new MySqlConnection(connectionString);

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT userId, steamId64
                    FROM orders
                    JOIN users ON orders.userId = users.discordId
                    WHERE delivered = false
                    AND presence = 'online'
                    ORDER BY orders.createdAt LIMIT 1
                ";

                try
                {
                    // Open the connection and fetch player with pending orders.
                    connection.Open();
                    var reader = cmd.ExecuteReader();

                    // When there are no players waiting for an ordr, do nothing.
                    if (!reader.HasRows)
                    {
                        DebugLog($"Nothing to deliver (DoingDelivery={DoingDelivery})");
                        return;
                    }

                    // Deliveries to perform.
                    DoingDelivery = true;

                    reader.Read();
                    var userid = reader.GetString("userId");
                    var steamid = reader.GetString("steamId64");
                    DebugLog($"Fetching orders for {userid} ({steamid})");

                    // Fetch all the orders for this player.
                    ordersConnection.Open();
                    var cmdOrders = ordersConnection.CreateCommand();
                    cmdOrders.CommandText = $"SELECT id, shopPackageId FROM orders WHERE userId = '{userid}' AND delivered = false;";
                    var ordersReader = cmdOrders.ExecuteReader();

                    while (ordersReader.Read())
                    {
                        var id = ordersReader.GetString("id");
                        var shopPackageId = ordersReader.GetInt32("shopPackageId");

                        DebugLog($"Processing Order {id} Package {shopPackageId}");

                        giveItemsConnection.Open();
                        var cmdGiveItems = giveItemsConnection.CreateCommand();
                        cmdGiveItems.CommandText = $"SELECT spawnName, qty FROM shop_items WHERE shopPackageId = {shopPackageId} AND spawnType = 'item'";
                        var readerGiveItems = cmdGiveItems.ExecuteReader();
                        while (readerGiveItems.Read())
                        {
                            var spawnName = readerGiveItems.GetString("spawnName");
                            var qty = readerGiveItems.GetInt32("qty");

                            DebugLog($"Spawned {qty} x {spawnName}");
                            ScumManager.SpawnItem(spawnName, qty, steamid);
                        }
                        giveItemsConnection.Close();

                        // Update delivery status for order.
                        connection_updatedelivery.Open();
                        var cmd_updatedelivery = connection_updatedelivery.CreateCommand();
                        cmd_updatedelivery.CommandText = $"UPDATE orders SET delivered=true WHERE id={id}";
                        cmd_updatedelivery.ExecuteNonQuery();
                        DebugLog($"Marked order {id} as delivered");
                        connection_updatedelivery.Close();
                    }

                    DebugLog("Done");
                }
                finally
                {
                    connection.Close();
                    ordersConnection.Close();
                }

                DoingDelivery = false;
            }
        }
    }
}
