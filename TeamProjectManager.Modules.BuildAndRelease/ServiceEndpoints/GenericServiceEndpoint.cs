using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System;

namespace TeamProjectManager.Modules.BuildAndRelease.ServiceEndpoints
{
    public class GenericServiceEndpoint
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Description { get; set; }

        public ServiceEndpoint ToServiceEndpoint()
        {
            var serviceEndpoint = new ServiceEndpoint
            {
                Name = this.Name,
                Type = ServiceEndpointTypes.Generic,
                Url = new Uri(this.Url),
                Description = this.Description,
                Authorization = new EndpointAuthorization
                {
                    Scheme = EndpointAuthorizationSchemes.UsernamePassword,
                }
            };
            serviceEndpoint.Authorization.Parameters.Add(EndpointAuthorizationParameters.Username, this.Username);
            serviceEndpoint.Authorization.Parameters.Add(EndpointAuthorizationParameters.Password, this.Password);
            return serviceEndpoint;
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(this.Name) && !string.IsNullOrWhiteSpace(this.Url) && !string.IsNullOrWhiteSpace(this.Username) && !string.IsNullOrWhiteSpace(this.Password);
        }
    }
}