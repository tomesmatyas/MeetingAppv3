using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeetingApp.Models;
using MeetingApp.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace MeetingApp.ViewModels
{
    public partial class TestViewModel : ObservableObject
    {
        private readonly MeetingService _meetingService;

        // Observable kolekce schùzek v UI.
        public ObservableCollection<Meeting> Meetings { get; } = new();

        // Pomocná vlastnost pro indikaci probíhající operace.
        [ObservableProperty]
        private bool isBusy;

        // Vlastnosti pro zadání nové schùzky.
        [ObservableProperty]
        private string newMeetingTitle;

        [ObservableProperty]
        private DateTime newMeetingDate = DateTime.Now;

        [ObservableProperty]
        private bool newMeetingIsRegular;
       
        public TestViewModel(MeetingService meetingService)
        {
            _meetingService = meetingService;
        }
        //ICommand LoadMeetingsAsyncCommand;
        // Pøíkaz pro naètení schùzek z API.
        
        [RelayCommand]
        public async Task LoadMeetingsAsync()
        {
            if (IsBusy)
                return;

            try
            {
                Debug.WriteLine("tlaèítko spuštìno");
                IsBusy = true;
                var meetings = await _meetingService.GetMeetingsAsync();
                Meetings.Clear();
                if (meetings != null)
                {
                    foreach (var meeting in meetings)
                    {
                        Meetings.Add(meeting);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Chyba pøi naèítání schùzek: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Pøíkaz pro pøidání nové schùzky.
        [RelayCommand]
        public async Task AddMeetingAsync()
        {
            if (IsBusy)
                return;

            if (string.IsNullOrWhiteSpace(NewMeetingTitle))
            {
                Debug.WriteLine("Název schùzky nesmí být prázdný");
                return;
            }

            try
            {
                Debug.WriteLine("tlaèítko spuštìno");
                IsBusy = true;
                var newMeeting = new Meeting
                {
                    Title = NewMeetingTitle,
                    Date = NewMeetingDate,
                    IsRegular = NewMeetingIsRegular
                };

                var result = await _meetingService.AddMeetingAsync(newMeeting);
                if (result)
                {
                    // Po úspìšném pøidání schùzky obnovíme seznam.
                    await LoadMeetingsAsync();
                }
                else
                {
                    Debug.WriteLine("Nepodaøilo se pøidat novou schùzku.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Výjimka pøi pøidávání schùzky: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
                // Vyèistíme vstupní pole.
                NewMeetingTitle = string.Empty;
                NewMeetingDate = DateTime.Now;
                NewMeetingIsRegular = false;
            }
        }

        // Pøíkaz pro smazání schùzky. Pøedáváme jako parametr celou schùzku.
        [RelayCommand]
        public async Task DeleteMeetingAsync(Meeting meeting)
        {
            if (meeting == null)
                return;

            try
            {
                IsBusy = true;
                var result = await _meetingService.DeleteMeetingAsync(meeting.Id);
                if (result)
                {
                    Meetings.Remove(meeting);
                }
                else
                {
                    Debug.WriteLine("Nepodaøilo se smazat schùzku.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Výjimka pøi mazání schùzky: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
