using CompMs.App.MsdialServer.Data;
using MudBlazor.Services;
using CompMs.App.MsdialServer.Project.View;
using CompMs.App.MsdialServer.Project.Model;
using CompMs.App.MsdialServer.Project.Service;
using CompMs.App.MsdialServer.Dataset.Model;

namespace CompMs.App.MsdialServer
{
    public class Program
    {
        public static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddSingleton<WeatherForecastService>();
            builder.Services.AddMudServices();

            // Project
            builder.Services.AddScoped<ProjectModel>();
            builder.Services.AddScoped<ILoadProjectDataService, LoadProjectDataService>();
            // ProjectDataFile
            builder.Services.AddScoped<IListProjectDataViewModel, ListProjectDataViewModel>();
            builder.Services.AddScoped<ISearchProjectDataModel, SearchProjectDataModel>();
            // Dataset
            builder.Services.AddScoped<DatasetModel>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment()) {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.Run();
        }
    }
}