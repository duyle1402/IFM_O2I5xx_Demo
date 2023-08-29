/*
 * This program is developed by DL for demo purpose
 * Email: prochief006@gmail.com
 * Date: fisrt demo release 08-2023
 * *
 * Description: Demo Connect IFM Vision O2I5xx with C# using external trigger (optical sensor)
 */
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Test_IFM
{
    class Program
    {
        private static TcpClient client;
        private static NetworkStream clientStream;
        private static readonly byte[] data = new byte[1024];       
        public static event Action<string> DataReceived;
        private const string EmptyString = "star;0;;;stop"; // my sensor return this string when no code is detected
        private const string IPAdress = "192.168.64.2"; // change IP address if needed
        private const int PCIC_Port = 50010; // default port of IFM PCIC
        static void Main(string[] args)
        {
            try
            {
                client = new TcpClient(IPAdress, PCIC_Port);
                clientStream = client.GetStream();

                // Khởi động luồng lắng nghe dữ liệu
                Thread listenThread = new Thread(ListenForData);
                listenThread.Start();

                // Chờ sự kiện dữ liệu nhận được và xử lý
                DataReceived += HandleReceivedData;

                // Vòng lặp chính hoặc bất kỳ xử lý khác 
                while (true)
                {
                    // Thực hiện các thao tác khác ở đây
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while listening for data: " + ex.Message);
                // process lost connection here
            }

        }
       
        private static void ListenForData()
        {
            try 
            {
                while (true)
                {
                    
                    int bytesRead;
                    // Read the first batch of the TcpServer response bytes. 
                    bytesRead = clientStream.Read(data, 0, data.Length);
                    string receivedData = Encoding.ASCII.GetString(data, 0, bytesRead);

                    // Read the second batch of the TcpServer response bytes.
                    bytesRead = clientStream.Read(data, 0, data.Length);
                    receivedData += Encoding.ASCII.GetString(data, 0, bytesRead);

                    // response data sampel: 2 codes with content "ABC-abc-1234"
                    //1234L000000043{0D}{0A}1234star;1;ABC-abc-1234;ABC-abc-1234;stop
                    //ticket + length + /r/n + ticket + data + /r/n
                    //extract data from receive string
                    //extract length information
                    int lengthData = int.Parse(receivedData.Substring(5, 9));
                    string stringData = receivedData.Substring(20, (lengthData - 6));

                    // Raise the DataReceived event
                    OnDataReceived(stringData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while listening for data: " + ex.Message);
                // process lost connection here
            }
            finally
            {
                client.Close();
                clientStream.Close();
                Console.WriteLine("Connection closed.");
            }

        }

        private static void OnDataReceived(string receivedData)
        {
            // Khi dữ liệu nhận được, gọi sự kiện DataReceived
            DataReceived?.Invoke(receivedData);
        }
        private static void HandleReceivedData(string receivedData)
        {
            try
            {
               
                if (receivedData.StartsWith("star") && receivedData.EndsWith("stop"))
                {
                    // data is empty
                    if (receivedData == EmptyString)
                    {
                        // cant find the code
                        Console.WriteLine("Code is Empty!");
                    }
                    else
                    {
                        string[] parts = receivedData.Split(';');
                        string qrCode = parts[2];
                        string content = parts[3];  
                        if(parts.Length >= 4)
                        {                            
                            Console.WriteLine("Detected {0} with content:{1} " , qrCode, content);
                        }
                        else 
                        {                           
                            Console.WriteLine("Data is invalid, use IFM vision assistant config sensor and try again!");                                
                        }
                    }                   
                }
                else 
                { 
                    Console.WriteLine("Data is invalid, use IFM vision assistant confid sensor and try again !");
                }               
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
      
    }
}
