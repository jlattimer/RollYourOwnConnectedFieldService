using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace SendParticleCommand
{
    public class Send : PluginBase
    {
        #region Constructor/Configuration
        private string _secureConfig = null;
        private string _unsecureConfig = null;

        public Send(string unsecure, string secureConfig)
            : base(typeof(Send))
        {
            _secureConfig = secureConfig;
            _unsecureConfig = unsecure;
        }
        #endregion

        private bool _isError;

        protected override void ExecuteCrmPlugin(LocalPluginContext localContext)
        {
            if (localContext == null)
                throw new ArgumentNullException(nameof(localContext));

            // Access token from the Particle.io Console should be sent in the secure config
            string accessToken = _secureConfig;
            if (string.IsNullOrEmpty(accessToken))
                throw new InvalidPluginExecutionException("Missing Particle Access Token");

            if (localContext.PluginExecutionContext.MessageName != "Create")
                throw new InvalidPluginExecutionException("Plug-in can only be registered on Create");

            Entity target = (Entity)localContext.PluginExecutionContext.InputParameters["Target"];

            if (target.LogicalName != "lat_particlecommand")
                throw new InvalidPluginExecutionException("Plug-in can only be registered on Particle Command");

            string function = GetParticleFunctionName(target.GetAttributeValue<OptionSetValue>("lat_particlefunction"));

            Entity iotDevice = localContext.OrganizationService.Retrieve("lat_iotdevice",
                target.GetAttributeValue<EntityReference>("lat_iotdevice").Id, new ColumnSet("lat_deviceid"));

            string deviceId = iotDevice.GetAttributeValue<string>("lat_deviceid");

            string requestBody = null;
            if (function == "setheatlevel")
                requestBody = CreateRequestBody(target.GetAttributeValue<OptionSetValue>("lat_newparticleheaterlevel"));

            var task = Task.Run(async () => await SendCommand(function, deviceId, requestBody, accessToken));
            Task.WaitAll(task);

            string response = task.Result;

            UpdateParticleCommand(target.Id, _isError ? 807990002 : 807990001, response, localContext.OrganizationService);

            ParticleResponse particleResponse = Serialization.DeserializeObject<ParticleResponse>(response);

            EntityReference device = target.GetAttributeValue<EntityReference>("lat_iotdevice");
            bool? heaterStatus = null;
            if (function == "heateron")
                heaterStatus = true;
            if (function == "heateroff")
                heaterStatus = false;

            int? heaterLevel = null;
            if (function == "getheatlevel")
                heaterLevel = particleResponse.return_value;
            if (function == "setheatlevel")
                heaterLevel = target.GetAttributeValue<OptionSetValue>("lat_newparticleheaterlevel").Value;

            int? temp = null;
            if (function == "gettemp")
                temp = particleResponse.return_value;

            int? humid = null;
            if (function == "gethumid")
                humid = particleResponse.return_value;

            int? battery = null;
            if (function == "getcharge")
                battery = particleResponse.return_value;


            UpdateIoTDevice(device.Id, heaterStatus, heaterLevel, temp, humid, battery, localContext.OrganizationService);
        }

        private static void UpdateIoTDevice(Guid deviceId, bool? heaterStatus, int? heaterLevel,
            int? temp, int? humid, int? battery, IOrganizationService service)
        {
            Entity iotDevice = new Entity("lat_iotdevice") { Id = deviceId };

            if (heaterStatus.HasValue)
            {
                iotDevice["lat_lastheaterstatus"] = heaterStatus.Value;
                iotDevice["lat_lastheaterstatusdate"] = DateTime.UtcNow;
            }

            if (heaterLevel.HasValue)
            {
                iotDevice["lat_lastheaterlevel"] = new OptionSetValue(heaterLevel.Value);
                iotDevice["lat_lastheaterleveldate"] = DateTime.UtcNow;
            }

            if (temp.HasValue)
            {
                iotDevice["lat_lastmanualtemperature"] = temp.Value;
                iotDevice["lat_lastmanualtemperaturedate"] = DateTime.UtcNow;
            }

            if (humid.HasValue)
            {
                iotDevice["lat_lastmanualhumidity"] = humid.Value;
                iotDevice["lat_lastmanualhumiditydate"] = DateTime.UtcNow;
            }

            if (battery.HasValue)
            {
                iotDevice["lat_lastmanualbatterycharge"] = battery.Value;
                iotDevice["lat_lastmanualbatterychargedate"] = DateTime.UtcNow;
            }

            iotDevice["lat_lastcontact"] = DateTime.UtcNow;

            service.Update(iotDevice);
        }

        private static void UpdateParticleCommand(Guid particleCommandId, int status, string response, IOrganizationService service)
        {
            Entity particleCommand = new Entity("lat_particlecommand")
            {
                Id = particleCommandId,
                ["lat_commandstatus"] = new OptionSetValue(status),
                ["lat_commandresponse"] = response,
                ["statecode"] = new OptionSetValue(1),
                ["statuscode"] = new OptionSetValue(2)
            };

            service.Update(particleCommand);
        }

        private async Task<string> SendCommand(string function, string deviceId, string requestBody, string accessToken)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Connection", "close");
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post,
                        new Uri("https://api.particle.io/v1/devices/" + deviceId + "/" + function + "?access_token=" + accessToken));

                if (requestBody != null)
                {
                    request.Content = new StringContent(requestBody);
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                }
                HttpResponseMessage response = await httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    _isError = true;

                return response.Content.ReadAsStringAsync().Result;
            }
        }

        private static string CreateRequestBody(OptionSetValue input)
        {
            return "{ \"arg\": \"" + input.Value + "\" }";
        }

        private static string GetParticleFunctionName(OptionSetValue function)
        {
            switch (function.Value)
            {
                case 807990000: //Turn heater on
                    return "heateron";
                case 807990001: //Turn heater off
                    return "heateroff";
                case 807990002: //Set heater level
                    return "setheatlevel";
                case 807990003: //Get heater level
                    return "getheatlevel";
                case 807990004: //Get temperature
                    return "gettemp";
                case 807990005: //Get humidity
                    return "gethumid";
                case 807990006: //Reset
                    return "reset";
                case 807990007: //Get battery charge 
                    return "getcharge";
            }

            return null;
        }
    }
}