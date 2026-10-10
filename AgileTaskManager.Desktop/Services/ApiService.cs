using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace AgileTaskManager.Desktop.Services
{
    public class ApiService
    {
        private readonly HttpClient _client;
        private readonly string _baseUrl;

        public ApiService()
        {
            _client = AppConfig.Client;
            _baseUrl = AppConfig.ApiBaseUrl;
        }

        // --- DỰ ÁN (PROJECTS) ---
        public async Task<List<T>> GetProjectsAsync<T>() => 
            await _client.GetFromJsonAsync<List<T>>($"{_baseUrl}/Projects");
            
        public async Task<HttpResponseMessage> CreateProjectAsync(object project) => 
            await _client.PostAsJsonAsync($"{_baseUrl}/Projects", project);

        // --- NGƯỜI DÙNG (USERS) ---
        public async Task<List<T>> GetUsersAsync<T>() => 
            await _client.GetFromJsonAsync<List<T>>($"{_baseUrl}/Users");
            
        public async Task<HttpResponseMessage> RegisterUserAsync(object user) => 
            await _client.PostAsJsonAsync($"{_baseUrl}/Users/register", user);

        // --- CỘT KANBAN (COLUMNS) ---
        public async Task<List<T>> GetColumnsAsync<T>(int projectId) => 
            await _client.GetFromJsonAsync<List<T>>($"{_baseUrl}/Columns/project/{projectId}");
            
        public async Task<HttpResponseMessage> CreateColumnAsync(object column) => 
            await _client.PostAsJsonAsync($"{_baseUrl}/Columns", column);
            
        public async Task<HttpResponseMessage> DeleteColumnAsync(int columnId) => 
            await _client.DeleteAsync($"{_baseUrl}/Columns/{columnId}");
            
        public async Task<HttpResponseMessage> ReorderColumnsAsync(object reorderList) => 
            await _client.PutAsJsonAsync($"{_baseUrl}/Columns/reorder", reorderList);

        // --- CÔNG VIỆC (TASKS) ---
        public async Task<List<T>> GetTasksAsync<T>(int projectId) => 
            await _client.GetFromJsonAsync<List<T>>($"{_baseUrl}/Tasks/project/{projectId}");
            
        public async Task<HttpResponseMessage> CreateTaskAsync(object task) => 
            await _client.PostAsJsonAsync($"{_baseUrl}/Tasks", task);
            
        public async Task<HttpResponseMessage> DeleteTaskAsync(int taskId) => 
            await _client.DeleteAsync($"{_baseUrl}/Tasks/{taskId}");
            
        public async Task<HttpResponseMessage> UpdateTaskColumnAsync(int taskId, int newColumnId) => 
            await _client.PatchAsJsonAsync($"{_baseUrl}/Tasks/{taskId}/column", newColumnId);

        public async Task<HttpResponseMessage> UpdateTaskAsync(int taskId, object task) => 
            await _client.PutAsJsonAsync($"{_baseUrl}/Tasks/{taskId}", task);
            
        public async Task<HttpResponseMessage> ReorderTasksAsync(object reorderList) => 
            await _client.PutAsJsonAsync($"{_baseUrl}/Tasks/reorder", reorderList);
    }
}
