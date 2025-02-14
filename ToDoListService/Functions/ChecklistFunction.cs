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
        [OpenApiOperation(operationId: "CreateChecklist", tags: new[] { "Checklist" })]
        [OpenApiRequestBody("application/json", typeof(CreateCheckListRequest), Required = true, Description = "Checklist Title")]
        [OpenApiResponseWithBody(HttpStatusCode.Created, "application/json", typeof(Checklist), Description = "Checklist created successfully")]
        public async Task<HttpResponseData> CreateChecklist(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            var requestBody = await req.ReadFromJsonAsync<CreateCheckListRequest>();
            if (requestBody == null || string.IsNullOrWhiteSpace(requestBody.Title))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Title is required");
                return badRequestResponse;
            }

            var checklist = _checklistService.CreateChecklist(requestBody.Title);
            _checklists.Add(checklist);
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(checklist);
            return response;
        }

        [Function("GetAllChecklists")]
        [OpenApiOperation(operationId: "GetAllChecklists", tags: new[] { "Checklist" })]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<Checklist>), Description = "List of all checklists")]
        public async Task<HttpResponseData> GetAllChecklists(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
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
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "checklist/{id}")] HttpRequestData req,
        int id)
        {
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
        [OpenApiRequestBody("application/json", typeof(ChecklistItem), Required = true, Description = "Checklist Item Data")]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Checklist), Description = "Checklist Updated")]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(string), Description = "Checklist Not Found")]
        public async Task<HttpResponseData> AddChecklistItem(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "checklists/{checklistId}/items")] HttpRequestData req,
    int checklistId)
        {
            var response = req.CreateResponse();

            if (_checklists == null || !_checklists.Any())
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteStringAsync("No checklists found.");
                return response;
            }

            var checklist = _checklists.FirstOrDefault(c => c.Id == checklistId);
            if (checklist == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteStringAsync($"Checklist with ID {checklistId} not found.");
                return response;
            }

            using var reader = new StreamReader(req.Body, Encoding.UTF8);
            var bodyString = await reader.ReadToEndAsync();
            var newItem = JsonSerializer.Deserialize<ChecklistItem>(bodyString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (newItem == null || string.IsNullOrWhiteSpace(newItem.Name))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Invalid item data.");
                return response;
            }

            int newItemId = checklist.Items.Any() ? checklist.Items.Max(i => i.Id) + 1 : 1;
            newItem.Id = newItemId;

            checklist.Items.Add(newItem);

            response.StatusCode = HttpStatusCode.OK;
            await response.WriteAsJsonAsync(checklist);
            return response;
        }
    }
}
