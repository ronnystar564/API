using Newtonsoft.Json.Linq;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace HonkHonkAPI2.Controllers
{
    public class getDataController : ApiController
    {
        public string jsonFormatter(DataSet ds)
        {
            string jsonString = string.Empty;
            if (ds != null && ds.Tables[0] != null)
            {
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    jsonString += "\"PostalCode" + i + 1 + "\":\"" + ds.Tables[0].Rows[i][0].ToString() + "\",";
                }

            }
            return jsonString;
        }

        //returns the partial match postcodes
        [AcceptVerbs("POST")]
        [Route("api/checkPostalCode")]
        [HttpPost]
        // POST: api/getData
        public JObject Post([FromBody] JObject data)
        {
            string jsonString = String.Empty;
            string dataString = string.Empty;
            DataSet ds = new DataSet();

            string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=test;Database=honkhonk";

            if (data.HasValues)
            {
                if (data["postalCode"] != null)
                {
                    NpgsqlDataAdapter nsda = new NpgsqlDataAdapter("select pcd from  HONKHONK where pcd like '%" + data["postalCode"].ToString() + "%'", connectionString);
                    nsda.Fill(ds);
                }

            }
            dataString = jsonFormatter(ds);
            jsonString = "{\"PostalCodes\": [{" + dataString.Substring(0, dataString.Length - 1) + "}]}";

            return JObject.Parse(jsonString);

        }
        //returns the neartest address
        [HttpPost]
        [Route("api/checkNearestaddress")]
        public JObject CheckNearestAddress([FromBody] JObject data)
        {
            if (data == null || data["postalCode"] == null)
            {
                return JObject.FromObject(new { error = "Postal code not provided" });
            }

            string postalCode = data["postalCode"].ToString();
            string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=test;Database=honkhonk";
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT lat, long FROM HONKHONK WHERE pcd LIKE @PostalCode";
                    NpgsqlCommand command = new NpgsqlCommand(query, connection);
                    command.Parameters.AddWithValue("@PostalCode", "%" + postalCode + "%");

                    DataSet ds = new DataSet();
                    NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(command);
                    adapter.Fill(ds);

                    JArray resultArray = new JArray();
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        JObject obj = new JObject();
                        obj["latitude"] = row["lat"].ToString();
                        obj["longitude"] = row["long"].ToString();
                        resultArray.Add(obj);
                    }

                    JObject jsonResponse = new JObject();
                    jsonResponse["PostalCodes"] = resultArray;

                    return jsonResponse;
                }
                catch (Exception)
                {
                    return JObject.FromObject(new { error = "An error occurred while processing your request." });
                }
            }
        }
    }
}
