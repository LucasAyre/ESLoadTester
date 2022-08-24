using System;
using System.Text;


namespace API_Load_Test
{

    public class APICalls
    {
        int Znumber;

        public APICalls(int number)
        {
            this.Znumber = number;
        }
        public void MakeAPICall()
        {
            HTTPRequest HTTP_GetAuth = new HTTPRequest("https://docs.earthsense.co.uk/authapi/api/AuthUser");
            HttpResponseMessage Response_GetAuth = HTTP_GetAuth.getRequest_GetAuth("LucasAyre", "UAjJIT0Mdkpm2nZb");
            string ResponseString_GetAuth = Response_GetAuth.Content.ReadAsStringAsync().Result;
            List<DateTime> StartDateList = new List<DateTime>();
            for (int i = 0;i< 100; i++)
            {
                Program p = new Program();
                DateTime randomDay = p.RandomDay();
                StartDateList.Add(randomDay);
                HTTPRequest HTTP_BearerToken = new HTTPRequest(p.HttpRequestString(randomDay));
                HttpResponseMessage Response_BearerToken = HTTP_BearerToken.getRequest_BearerToken(ResponseString_GetAuth);
            }
            foreach (DateTime startDate in StartDateList)
            {
                Console.WriteLine(startDate.ToString());
            }
        }
    }


    public class Program
    {
        public Random gen = new Random();
        public DateTime RandomDay()
        {
            DateTime start = new DateTime(2022, 4, 22);
            int range = (DateTime.Today - start).Days;
            return start.AddDays(gen.Next(range));
        }




        public string HttpRequestString(DateTime StartDate)
        {
            DateTime EndDate = new DateTime();
            EndDate = StartDate.AddDays(7);
            
            string URI = $"https://docs.earthsense.co.uk/vzephyrapi/api/GetData/?vzephyr_id=180&start_dt={StartDate.ToString()}T08%3A41%3A00&end_dt={EndDate.ToString()}T08%3A41%3A00";

            return URI.ToString();
        }


        static void Main(string[] args)
        {
            Program p = new Program();
            HTTPRequest HTTP_GetAuth = new HTTPRequest("https://docs.earthsense.co.uk/authapi/api/AuthUser");
            HttpResponseMessage Response_GetAuth = HTTP_GetAuth.getRequest_GetAuth("LucasAyre", "UAjJIT0Mdkpm2nZb");
            string ResponseString_GetAuth = Response_GetAuth.Content.ReadAsStringAsync().Result;



            List<APICalls> ListOfAPICalls = new List<APICalls>();

            int _threadLimit = 100;

            //Phase 1 - Initialise object (list of objects)
            for (int i = 0; i < _threadLimit; i++)
            {
                ListOfAPICalls.Add(new APICalls(i));
            }

            //Phase 2 - Assign to thread
            List<ThreadStart> ThreadStartList = new List<ThreadStart>();
            List<Thread> ThreadList = new List<Thread>();

            for (int i = 0; i < _threadLimit; i++)
            {
                ThreadStartList.Add(ListOfAPICalls[i].MakeAPICall);
                ThreadList.Add(new Thread(ThreadStartList[i]));
            }

            //Phase 3 - Start threads
            for (int i = 0; i < _threadLimit; i++)
            {
                ThreadList[i].Start();
            }

            for (int i = 0; i < _threadLimit; i++)
            {
                ThreadList[i].Join();
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
        public HttpResponseMessage getRequest_BearerToken(string BearerToken)
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
