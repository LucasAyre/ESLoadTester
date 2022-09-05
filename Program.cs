using System;
using System.Text;
using System.Diagnostics;
using System.Net;
using System.CommandLine;
using CommandLine;
using System.IO;

namespace API_Load_Test
{

    public class APICalls
    {
        public List<int> ByteSizesList;
        public List<float> APICallsTimes;
        public int FailedCalls = 0;
        public string BearerToken;
        public int NumberOfCalls;
        public DateTime Start;
        public string GetDataURI;
        public string filepath;
        public APICalls(string BearerToken,int NumberOfCalls,DateTime Start,string GetDataURI,string filepath)
        {
            this.BearerToken = BearerToken;
            this.NumberOfCalls = NumberOfCalls;
            this.Start = Start;
            this.GetDataURI = GetDataURI;
            this.filepath = filepath;
        }

        public void MakeAPICall()
        {
            List<int> totalStatuses = new List<int>() { 0,0,0,0 };



            DateTime Calls_StartTime = DateTime.Now;
            int TotalNumberOfCallsCompleted = 0;
            List<int> BinningByteSizesList = new List<int>();
            List<float> BinningAPICallsTimes = new List<float>();
            double previousTime = 0;
            Stopwatch timeCounter = Stopwatch.StartNew();
            int count = 0;
            int TotalByteSizes = 0;
            ByteSizesList = new List<int>();
            Program p = new Program();

            TimeSpan TotalTimes = TimeSpan.Zero;
            APICallsTimes = new List<float>();
            bool changeToSeconds = false;
            if (NumberOfCalls < 20)
            {
                changeToSeconds = true;
            }            
            
            for (int i = 0;i< NumberOfCalls ; i++)
            {

                GetRequestStatus status = new GetRequestStatus(totalStatuses);


                Stopwatch APICallStopwatch = new Stopwatch();

                DateTime StartDate = p.GetStartDate(Start);
                HTTPRequest HTTP_GetData = new HTTPRequest(p.GetURI(StartDate,GetDataURI));

                APICallStopwatch.Start();
                HttpResponseMessage Response_GetData = HTTP_GetData.getRequestGetData(BearerToken);
                APICallStopwatch.Stop();

                HttpStatusCode code = Response_GetData.StatusCode;
                Byte[] Bytes = Response_GetData.Content.ReadAsByteArrayAsync().Result;
                int ByteSize = Bytes.Length;
                totalStatuses = status.DetermineStatus((int)code);
                
                if ((int)code > 200 | (int)code < 200)
                {
                    FailedCalls += 1;
                }
                

                ByteSizesList.Add(ByteSize);
                BinningByteSizesList.Add(ByteSize);


                APICallsTimes.Add(APICallStopwatch.ElapsedMilliseconds);
                BinningAPICallsTimes.Add(APICallStopwatch.ElapsedMilliseconds);
                TotalNumberOfCallsCompleted = TotalNumberOfCallsCompleted + 1;


                TimeSpan timeSpan = DateTime.Now - Calls_StartTime;



                if (changeToSeconds && Math.Truncate(timeSpan.TotalSeconds) > previousTime)
                {
                    previousTime = Math.Truncate(timeSpan.TotalSeconds);
                    Console.WriteLine($"{DateTime.Now,-23}{Math.Round(BinningAPICallsTimes.Average(), 2).ToString("0.00"),-10}{BinningAPICallsTimes.Count,-10}{Math.Round(BinningByteSizesList.Average(), 2).ToString("0.00"),-10}{BinningByteSizesList.Count.ToString(),-15}{Math.Truncate(timeSpan.TotalSeconds),-15}{totalStatuses[0],-5}{totalStatuses[1],-5}{totalStatuses[2],-5}{totalStatuses[3],-15}{TotalNumberOfCallsCompleted,-10}");
                    BinningByteSizesList.Clear();
                    BinningAPICallsTimes.Clear();

                }


                if (changeToSeconds == false && Math.Truncate(timeSpan.TotalMinutes) > previousTime) //Change Elapsed.Minutes to Elapsed.Seconds or vice versa to change how often the program outputs the stats
                {
                    previousTime = Math.Truncate(timeSpan.TotalMinutes);
                    Console.WriteLine($"{DateTime.Now,-23}{Math.Round(BinningAPICallsTimes.Average(), 2).ToString("0.00"),-10}{BinningAPICallsTimes.Count,-10}{Math.Round(BinningByteSizesList.Average(), 2).ToString("0.00"),-10}{BinningByteSizesList.Count.ToString(),-15}{Math.Truncate(timeSpan.TotalMinutes),-15}{totalStatuses[0],-5}{totalStatuses[1],-5}{totalStatuses[2],-5}{totalStatuses[3],-15}{TotalNumberOfCallsCompleted,-10}");
                    BinningByteSizesList.Clear();
                    BinningAPICallsTimes.Clear();
                }
            }
        }
    }


    public class Program
    {
        public List<int> ThreadLimit = new List<int> {};
        public List<int> CallsPerThread = new List<int> {};


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
            List<int> NumberOfCalls = p.CallsPerThread;
            List<int> threadLimit = p.ThreadLimit;
            DateTime Start = new DateTime(2022, 4, 21);
            string GetDataURI_NoDates = "https://service-dev.earthsense.co.uk/virtualzephyr/api/GetData/?vzephyr_id=180";
            string getAuthURI = "https://service.earthsense.co.uk/authapi/api/AuthUser";
            string filename = "/APICallsOutput.csv";
            string directory = Directory.GetCurrentDirectory();
            Console.WriteLine(directory + filename);
            if (!File.Exists(directory + filename)) 
            { 
                File.Create(directory + filename);
            }


            p.ParseArguments(args);


            HTTPRequest http_GetAuth = new HTTPRequest(getAuthURI);
            HttpResponseMessage response_GetAuth = http_GetAuth.getRequest_GetAuth("LucasAyre", "UAjJIT0Mdkpm2nZb");
            string responseString_GetAuth = response_GetAuth.Content.ReadAsStringAsync().Result;
            File.WriteAllText(directory+filename, "");
            for (int j =0; j < threadLimit.Count; j++)
            {
                List<APICalls> listOfAPICalls = new List<APICalls>();


                //Phase 1 - Initialise object (list of objects)
                for (int i = 0; i < threadLimit[j]; i++)
                {
                    listOfAPICalls.Add(new APICalls(responseString_GetAuth, NumberOfCalls[j],Start,GetDataURI_NoDates,directory + filename));
                }

                //Phase 2 - Assign to thread
                List<ThreadStart> threadStartList = new List<ThreadStart>();
                List<Thread> threadList = new List<Thread>();

                for (int i = 0; i < threadLimit[j]; i++)
                {
                    threadStartList.Add(listOfAPICalls[i].MakeAPICall);
                    threadList.Add(new Thread(threadStartList[i]));
                }

                //Phase 3 - Start threads
                for (int i = 0; i < threadLimit[j];i ++)
                {
                    threadList[i].Start();
                }
                Console.WriteLine("");
                Console.WriteLine($"{"DateTime",-25}{"ms",-22}{"b",-17}{"mins/secs",-17}{"200",-5}{"300",-5}{"400",-5}{"500",-14}{"Total Calls"}");

                for (int i = 0; i < threadLimit[j]; i++)
                {
                    while (threadList[i].IsAlive) { }
                }

                List<float> overallAverageCallTimes = new List<float>();
                List<double> overallAverageSizeOfResponse = new List<double>();
                int overallTotalNumberOfFailedCalls = new int();
                for (int i = 0; i < threadLimit[j]; i++)
                {
                    threadList[i].Join();
                    Console.WriteLine($"\nAverages for Thread {i+1}: ");
                    Console.WriteLine($"Average Length of API Call: {listOfAPICalls[i].APICallsTimes.Average()}(ms) Average Size of Response: {listOfAPICalls[i].ByteSizesList.Average()}(b) Number of Failed Calls:{listOfAPICalls[i].FailedCalls}");
                    overallAverageCallTimes.Add(listOfAPICalls[i].APICallsTimes.Average());
                    overallAverageSizeOfResponse.Add(listOfAPICalls[i].ByteSizesList.Average());
                    overallTotalNumberOfFailedCalls = overallTotalNumberOfFailedCalls + listOfAPICalls[i].FailedCalls;
                }
                Console.WriteLine($"\nAverages across each thread: ");
                string output = $"{overallAverageCallTimes.Average(),5}, {Math.Round(overallAverageSizeOfResponse.Average()),5}, {overallTotalNumberOfFailedCalls,5}";
                Console.WriteLine(output);

                File.AppendAllText(directory + filename,output);
                File.AppendAllText(directory + filename, "\n");
            }

            
        }
        void ParseArguments(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<ArgsOptions>(args).WithParsed<ArgsOptions>(o =>
            {
                if (o.UnsplitThreads != null)
                {
                    string[] StrThreadLimit = o.UnsplitThreads.Split(",");
                    for(int i = 0; i < StrThreadLimit.Length; i++)
                    {
                        ThreadLimit.Add(int.Parse(StrThreadLimit[i]));
                    }
                }

                
                if (o.UnsplitCallsPerThread != null)
                {
                    string[] strCallsPerThread = o.UnsplitCallsPerThread.Split(",");
                    for(int i = 0; i < strCallsPerThread.Length; i++)
                    {
                        CallsPerThread.Add(int.Parse(strCallsPerThread[i]));
                    }
                }
                
            });

        }
    }

    public class ArgsOptions
    {
        [CommandLine.Option("Threads", Required = true, HelpText = "Set number of threads")]
        public string UnsplitThreads { get; set; }


        [CommandLine.Option("CallsPerThread", Required = true, HelpText = "Set number of calls per thread")]
        public string UnsplitCallsPerThread { get; set; }
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
