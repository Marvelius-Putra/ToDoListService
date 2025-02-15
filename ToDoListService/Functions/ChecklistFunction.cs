using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ToDoListService.Model;
using ToDoListService.Services;
using ToDoListService.Interfaces;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;

namespace ToDoListService.Functions
{
    public class ChecklistFunction
    {
        private readonly ICheckListService _checklistService;
        private static List<Checklist> _checklists = new List<Checklist>();


        public ChecklistFunction(ICheckListService checklistService)
        {
            _checklistService = checklistService;
        }

        [Function("CreateChecklist")]
        [Authorize] 
        [OpenApiOperation(operationId: "CreateChecklist", tags: new[] { "Checklist" })]
        [OpenApiRequestBody("application/json", typeof(CreateCheckListRequest), Required = true, Description = "Checklist Title")]
        [OpenApiResponseWithBody(HttpStatusCode.Created, "application/json", typeof(Checklist), Description = "Checklist created successfully")]
        public async Task<HttpResponseData> CreateChecklist([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            //read json body
            var requestBody = await req.ReadFromJsonAsync<CreateCheckListRequest>();
            if (requestBody == null || string.IsNullOrWhiteSpace(requestBody.Title))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Title is required");
                return badRequestResponse;
            }

            //Create the Checklist
            var checklist = _checklistService.CreateChecklist(requestBody.Title);
            _checklists.Add(checklist);
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(checklist);
            return response;
        }

        [Function("GetAllChecklists")]
        [OpenApiOperation(operationId: "GetAllChecklists", tags: new[] { "Checklist" })]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<Checklist>), Description = "List of all checklists")]
        public async Task<HttpResponseData> GetAllChecklists([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            //get all checklist
            var checklists = _checklistService.GetAllChecklists();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(checklists);
            return response;
        }

        [Function("DeleteChecklist")]
        [OpenApiOperation(operationId: "DeleteChecklist", tags: new[] { "Checklist" })]
        [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "ID dari checklist yang ingin dihapus")]
        [OpenApiResponseWithoutBody(HttpStatusCode.NoContent, Description = "Checklist berhasil dihapus")]
        [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Checklist tidak ditemukan")]
        public async Task<HttpResponseData> DeleteChecklist(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "checklist/{id}")] HttpRequestData req, int id)
        {
            //delete checklist by id
            bool isDeleted = _checklistService.DeleteChecklist(id);

            if (!isDeleted)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("Checklist not found");
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.NoContent);
            return response;
        }

        
        [Function("AddChecklistItem")]
        [OpenApiOperation(operationId: "AddChecklistItem", tags: new[] { "Checklist" })]
        [OpenApiParameter(name: "checklistId", In = ParameterLocation.Path, Required = true, Type = typeof(int), Description = "ID Checklist")]
        [OpenApiRequestBody("application/json", typeof(CheckListItemRequest), Required = true, Description = "Checklist Item Data")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Checklist), Description = "Checklist Updated")]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(string), Description = "Checklist Not Found")]
        public async Task<HttpResponseData> AddChecklistItem([HttpTrigger(AuthorizationLevel.Function, "post", Route = "checklists/{checklistId}/items")] HttpRequestData req, int checklistId)
        {
            var response = req.CreateResponse();

            // Validate if checklist is not exist
            if (_checklists == null || !_checklists.Any())
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteStringAsync("No checklists found.");
                return response;
            }

            // Search checklist by id
            var checklist = _checklists.FirstOrDefault(c => c.Id == checklistId);
            if (checklist == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteStringAsync($"Checklist with ID {checklistId} not found.");
                return response;
            }

            // read the body request
            var requestBody = await req.ReadFromJsonAsync<CheckListItemRequest>();
            if (requestBody == null || string.IsNullOrWhiteSpace(requestBody.Name))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Invalid item data.");
                return response;
            }

            // Auto increament ID
            int newItemId = checklist.Items.Any() ? checklist.Items.Max(i => i.Id) + 1 : 1;

            // set isCompleted to false as default
            var newItem = new ChecklistItem
            {
                Id = newItemId,
                Name = requestBody.Name,
                IsCompleted = false 
            };

            // add item to checklist
            checklist.Items.Add(newItem);

            response.StatusCode = HttpStatusCode.OK;
            await response.WriteAsJsonAsync(checklist);
            return response;
        }


        [Function("UpdateMultipleChecklistItems")]
        [OpenApiOperation(operationId: "UpdateMultipleChecklistItems", tags: new[] { "Checklist" })]
        [OpenApiParameter(name: "checklistId", In = ParameterLocation.Path, Required = true, Type = typeof(int), Description = "ID Checklist")]
        [OpenApiRequestBody("application/json", typeof(List<UpdateChecklistItemRequest>), Required = true, Description = "List of Checklist Items to Update")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(UpdateChecklistResponse), Description = "Checklist Updated")]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(string), Description = "Checklist Not Found")]
        public async Task<HttpResponseData> UpdateMultipleChecklistItems([HttpTrigger(AuthorizationLevel.Function, "put", Route = "checklists/{checklistId}/items")] HttpRequestData req, int checklistId)
        {
            var response = req.CreateResponse();

            // find checklist by id
            var checklist = _checklists.FirstOrDefault(c => c.Id == checklistId);
            if (checklist == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteStringAsync($"Checklist with ID {checklistId} not found.");
                return response;
            }

            // Read the body request
            var requestBody = await req.ReadFromJsonAsync<List<UpdateChecklistItemRequest>>();
            if (requestBody == null || !requestBody.Any())
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Invalid item data.");
                return response;
            }

            //ignore if item is not exist to the list
            var ignoredItems = new List<int>(); 

            //update the item
            foreach (var updateItem in requestBody)
            {
                var item = checklist.Items.FirstOrDefault(i => i.Id == updateItem.Id);
                if (item != null)
                {
                    item.Name = updateItem.Name;
                }
                else
                {
                    ignoredItems.Add(updateItem.Id);
                }
            }

            // response payload
            var updateResponse = new UpdateChecklistResponse
            {
                Checklist = checklist,
                IgnoredItems = ignoredItems
            };

            response.StatusCode = HttpStatusCode.OK;
            await response.WriteAsJsonAsync(updateResponse);
            return response;
        }

        [Function("UpdateChecklistItemStatus")]
        [OpenApiOperation(operationId: "UpdateChecklistItemStatus", tags: new[] { "Checklist" })]
        [OpenApiParameter(name: "checklistId", In = ParameterLocation.Path, Required = true, Type = typeof(int), Description = "ID Checklist")]
        [OpenApiRequestBody("application/json", typeof(List<UpdateChecklistItemStatusRequest>), Required = true, Description = "List of Item Status Updates")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(UpdateChecklistResponse), Description = "Checklist Updated")]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(string), Description = "Checklist Not Found")]
        public async Task<HttpResponseData> UpdateChecklistItemStatus(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "checklists/{checklistId}/items/status")] HttpRequestData req, int checklistId)
        {
            var response = req.CreateResponse();

            // Find Checklist status by id
            var checklist = _checklists.FirstOrDefault(c => c.Id == checklistId);
            if (checklist == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteStringAsync($"Checklist with ID {checklistId} not found.");
                return response;
            }

            // Read Body request
            var requestBody = await req.ReadFromJsonAsync<List<UpdateChecklistItemStatusRequest>>();
            if (requestBody == null || !requestBody.Any())
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Invalid item data.");
                return response;
            }

            var ignoredItems = new List<int>(); 

            foreach (var updateItem in requestBody)
            {
                var item = checklist.Items.FirstOrDefault(i => i.Id == updateItem.Id);
                if (item != null)
                {
                    item.IsCompleted = updateItem.IsCompleted; 
                }
                else
                {
                    ignoredItems.Add(updateItem.Id);
                }
            }

            // Response payload
            var updateResponse = new UpdateChecklistResponse
            {
                Checklist = checklist,
                IgnoredItems = ignoredItems
            };

            response.StatusCode = HttpStatusCode.OK;
            await response.WriteAsJsonAsync(updateResponse);
            return response;
        }

        [Function("DeleteChecklistItem")]
        [OpenApiOperation(operationId: "DeleteChecklistItem", tags: new[] { "Checklist" })]
        [OpenApiParameter(name: "checklistId", In = ParameterLocation.Path, Required = true, Type = typeof(int), Description = "ID Checklist")]
        [OpenApiRequestBody("application/json", typeof(List<int>), Required = true, Description = "List of Item IDs to Delete")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(DeleteChecklistResponse), Description = "Checklist Updated")]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(string), Description = "Checklist Not Found")]
        public async Task<HttpResponseData> DeleteChecklistItem([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "checklists/{checklistId}/items")] HttpRequestData req, int checklistId)
        {
            var response = req.CreateResponse();

            // Find Checklist by id
            var checklist = _checklists.FirstOrDefault(c => c.Id == checklistId);
            if (checklist == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteStringAsync($"Checklist with ID {checklistId} not found.");
                return response;
            }

            // Read Body Request
            var requestBody = await req.ReadFromJsonAsync<List<int>>();
            if (requestBody == null || !requestBody.Any())
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Invalid item data.");
                return response;
            }

            var ignoredItems = new List<int>(); 
            var initialCount = checklist.Items.Count;

            // Delete Item on the checklist
            checklist.Items.RemoveAll(item =>
            {
                if (requestBody.Contains(item.Id))
                {
                    return true; 
                }
                else
                {
                    ignoredItems.Add(item.Id); 
                    return false;
                }
            });

            // Response Payload
            var deleteResponse = new DeleteChecklistResponse
            {
                Checklist = checklist,
                IgnoredItems = ignoredItems
            };

            response.StatusCode = HttpStatusCode.OK;
            await response.WriteAsJsonAsync(deleteResponse);
            return response;
        }

        [Function("GetChecklistItems")]
        [OpenApiOperation(operationId: "GetChecklistItems", tags: new[] { "Checklist" })]
        [OpenApiParameter(name: "checklistId", In = ParameterLocation.Path, Required = true, Type = typeof(int), Description = "ID Checklist")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<ChecklistItem>), Description = "List of items in the checklist")]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(string), Description = "Checklist Not Found")]
        public async Task<HttpResponseData> GetChecklistItems([HttpTrigger(AuthorizationLevel.Function, "get", Route = "checklists/{checklistId}/items")] HttpRequestData req, int checklistId)
        {
            var response = req.CreateResponse();

            // Find Checklist by ID
            var checklist = _checklists.FirstOrDefault(c => c.Id == checklistId);
            if (checklist == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteStringAsync($"Checklist with ID {checklistId} not found.");
                return response;
            }

            response.StatusCode = HttpStatusCode.OK;
            await response.WriteAsJsonAsync(checklist.Items);
            return response;
        }
    }
}
