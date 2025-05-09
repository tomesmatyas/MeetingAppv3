using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using MeetingAppv2.Services;

namespace MeetingAppv2.Services
{
    public class AlertService : IAlertService
    {
        private readonly Page _page;

        public AlertService(Page page)
        {
            _page = page;
        }

        public Task ShowAlertAsync(string title, string message, string cancel)
        {
            return _page.DisplayAlert(title, message, cancel);
        }
    }
}
public interface IAlertService
{
    Task ShowAlertAsync(string title, string message, string cancel);
}