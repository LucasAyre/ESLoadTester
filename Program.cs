using System;
using System.Text;
using System.Diagnostics;
using System.Net;

namespace API_Load_Test
{

    public class APICalls
    {
        public List<int> ByteSizesList;
        public List<float> APICallsTimes;
        public string BearerToken;
        public int NumberOfCalls;
        public DateTime Start;
        public string GetDataURI;

        public APICalls(string BearerToken,int NumberOfCalls,DateTime Start,string GetDataURI)
        {
            this.BearerToken = BearerToken;
            this.NumberOfCalls = NumberOfCalls;
            this.Start = Start;
            this.GetDataURI = GetDataURI;
            
        }

        public void MakeAPICall()
        {
            List<int> BinningByteSizesList = new List<int>();
            List<float> BinningAPICallsTimes = new List<float>();
            Stopwatch MinuteCounter = Stopwatch.StartNew();
            int count = 1;
            int TotalByteSizes = 0;
            ByteSizesList = new List<int>();
            Program p = new Program();

            TimeSpan TotalTimes = TimeSpan.Zero;
            APICallsTimes = new List<float>();
            for (int i = 0;i< NumberOfCalls ; i++)
            {
                Stopwatch APICallStopwatch = new Stopwatch();


                DateTime StartDate = p.GetStartDate(Start);
                HTTPRequest HTTP_GetData = new HTTPRequest(p.GetURI(StartDate,GetDataURI));

                APICallStopwatch.Start();
                HttpResponseMessage Response_GetData = HTTP_GetData.getRequestGetData(BearerToken);
                APICallStopwatch.Stop();

                HttpStatusCode code = Response_GetData.StatusCode;
                Byte[] Bytes = Response_GetData.Content.ReadAsByteArrayAsync().Result;
                int ByteSize = Bytes.Length;
                if (code == HttpStatusCode.OK)
                {
                    ByteSizesList.Add(ByteSize);
                    BinningByteSizesList.Add(ByteSize);
                }
                APICallsTimes.Add(APICallStopwatch.ElapsedMilliseconds);
                BinningAPICallsTimes.Add(APICallStopwatch.ElapsedMilliseconds);
                

                if (MinuteCounter.Elapsed.Seconds > count)
                {
                    Console.WriteLine($"{DateTime.Now,5}   {Math.Round(BinningAPICallsTimes.Average(),2).ToString("0.00"),5} {BinningAPICallsTimes.Count,5}  {Math.Round(BinningByteSizesList.Average(),2).ToString("0.00"),5}    {BinningByteSizesList.Count,5}  {count+1,5}");
                    BinningByteSizesList.Clear();
                    BinningAPICallsTimes.Clear();
                    count = count + 1;
                }
            }
        }
    }


    public class Program
    {
        public Random gen = new Random();


        public DateTime GetStartDate(DateTime start)
        {
            int range = (DateTime.Today - start).Days;
            return start.AddDays(gen.Next(range));
        }

        public string GetURI(DateTime StartDate,string _URI)
        {
            DateTime EndDate = new DateTime();
            EndDate = StartDate.AddDays(7);
            string URI = $"{_URI}&start_dt={StartDate.ToString("yyyy/MM/dd HH:mm:ss")}&end_dt={EndDate.ToString("yyyy/MM/dd HH:mm:ss")}";

            return URI.ToString();
        }

        static void Main(string[] args) 
        {
            Program p = new Program();

            int NumberOfCalls = 5;
            int ThreadLimit = 5;
            DateTime Start = new DateTime(2022, 8, 21);
            string GetDataURI_NoDates = "https://service-dev.earthsense.co.uk/vzephyrapi/api/GetData/?vzephyr_id=180";
            string? GetAuthURI = "https://service.earthsense.co.uk/authapi/api/AuthUser";

            HTTPRequest HTTP_GetAuth = new HTTPRequest(GetAuthURI);
            HttpResponseMessage Response_GetAuth = HTTP_GetAuth.getRequest_GetAuth("LucasAyre", "UAjJIT0Mdkpm2nZb");
            string ResponseString_GetAuth = Response_GetAuth.Content.ReadAsStringAsync().Result;

            List<APICalls> ListOfAPICalls = new List<APICalls>();


            //Phase 1 - Initialise object (list of objects)
            for (int i = 0; i < ThreadLimit; i++)
            {
                ListOfAPICalls.Add(new APICalls(ResponseString_GetAuth,NumberOfCalls,Start,GetDataURI_NoDates));
            }

            //Phase 2 - Assign to thread
            List<ThreadStart> ThreadStartList = new List<ThreadStart>();
            List<Thread> ThreadList = new List<Thread>();

            for (int i = 0; i < ThreadLimit; i++)
            {
                ThreadStartList.Add(ListOfAPICalls[i].MakeAPICall);
                ThreadList.Add(new Thread(ThreadStartList[i]));
            }

            //Phase 3 - Start threads
            for (int i = 0; i < ThreadLimit; i++)
            {
                ThreadList[i].Start();
            }
            string ms = "(ms)";
            string b = "(b)";
            Console.WriteLine($"{ms,25}{b,15}");

            for (int i = 0; i < ThreadLimit; i++)
            {
                while (ThreadList[i].IsAlive) { }
            }

            for (int i = 0; i < ThreadLimit; i++)
            {
                ThreadList[i].Join();
                Console.WriteLine($"\nAverages for Thread {i+1}: ");
                Console.WriteLine($"{ListOfAPICalls[i].APICallsTimes.Average()} {ListOfAPICalls[i].ByteSizesList.Average()}");
            }  
        }
    }



    public class HTTPRequest
    {
        HttpClient httpClient;
        string request_string;
        HttpRequestMessage request;
        List<string> HTTPRequestStrings = new List<string>();


        public HTTPRequest(string request_string)
        {
            this.httpClient = new HttpClient();
            this.request_string = request_string;
        }


        public HttpResponseMessage getRequest_GetAuth(string Username, string Password)
        {
            string auth_string_base64;

            request = new HttpRequestMessage(HttpMethod.Get, request_string);
            auth_string_base64 = Base64Encode($"{Username}:{Password}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth_string_base64);

            return httpClient.SendAsync(request).Result;
        }
        public HttpResponseMessage getRequestGetData(string BearerToken)
        {


            request = new HttpRequestMessage(HttpMethod.Get, request_string);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", BearerToken.ToString());

            return httpClient.SendAsync(request).Result;
        }


        public static string Base64Encode(string plainText)
        {
            byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}
