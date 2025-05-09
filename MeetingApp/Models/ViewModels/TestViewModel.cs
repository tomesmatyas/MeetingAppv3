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

        // Observable kolekce sch�zek v UI.
        public ObservableCollection<Meeting> Meetings { get; } = new();

        // Pomocn� vlastnost pro indikaci prob�haj�c� operace.
        [ObservableProperty]
        private bool isBusy;

        // Vlastnosti pro zad�n� nov� sch�zky.
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
        // P��kaz pro na�ten� sch�zek z API.
        
        [RelayCommand]
        public async Task LoadMeetingsAsync()
        {
            if (IsBusy)
                return;

            try
            {
                Debug.WriteLine("tla��tko spu�t�no");
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
                Debug.WriteLine("Chyba p�i na��t�n� sch�zek: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // P��kaz pro p�id�n� nov� sch�zky.
        [RelayCommand]
        public async Task AddMeetingAsync()
        {
            if (IsBusy)
                return;

            if (string.IsNullOrWhiteSpace(NewMeetingTitle))
            {
                Debug.WriteLine("N�zev sch�zky nesm� b�t pr�zdn�");
                return;
            }

            try
            {
                Debug.WriteLine("tla��tko spu�t�no");
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
                    // Po �sp�n�m p�id�n� sch�zky obnov�me seznam.
                    await LoadMeetingsAsync();
                }
                else
                {
                    Debug.WriteLine("Nepoda�ilo se p�idat novou sch�zku.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("V�jimka p�i p�id�v�n� sch�zky: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
                // Vy�ist�me vstupn� pole.
                NewMeetingTitle = string.Empty;
                NewMeetingDate = DateTime.Now;
                NewMeetingIsRegular = false;
            }
        }

        // P��kaz pro smaz�n� sch�zky. P�ed�v�me jako parametr celou sch�zku.
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
                    Debug.WriteLine("Nepoda�ilo se smazat sch�zku.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("V�jimka p�i maz�n� sch�zky: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
