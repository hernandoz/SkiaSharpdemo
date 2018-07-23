using Plugin.Media;
using Plugin.Media.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using TouchTrackingEffectDemos;
using Xamarin.Forms;

namespace TouchTrackingEffectDemos
{
    public partial class HomePage : ContentPage
    {
        public byte[] imageAsBytes;

        public HomePage()
        {
            InitializeComponent();

            NavigateCommand = new Command<Type>(async (Type pageType) =>
            {
                Page page = (Page)Activator.CreateInstance(pageType);
                await Navigation.PushAsync(page);
            });

            BindingContext = this;

            MessagingCenter.Subscribe<FingerPaintPage, byte[]>(this, "saveimage",  (sender, arg) =>
            {
                //imageAsBytes = arg;
                RefreshMediaList();
            });
        }

        public ICommand NavigateCommand { private set; get; }

        //Take Picture
        private async System.Threading.Tasks.Task Button_Pressed(object sender, EventArgs e)
        {
            if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
            {
                await DisplayAlert("No Camera", ":( No camera avaialble.", "OK");
                return;
            }

            var mediaOptions = new StoreCameraMediaOptions
            {
                Name = $"{DateTime.UtcNow}.jpg",
                PhotoSize = PhotoSize.Custom,
                CustomPhotoSize = 60,//Resize
            };

            using (var file = await CrossMedia.Current.TakePhotoAsync(mediaOptions))
            {
                if (file == null)
                    return;

                imageAsBytes = File.ReadAllBytes(file.Path);
                RefreshMediaList();
            }
        }

        public void RefreshMediaList()
        {
            MediaList.Children.Clear();

            var ContentAsImageSource = ImageSource.FromStream(() => new MemoryStream(imageAsBytes));

            Image photoimage = new Image
            {
                Source = ContentAsImageSource,
                WidthRequest = 200,
                HeightRequest = 400,
            };

             MediaList.Children.Add(photoimage);   
            
        }

        private async void Button_Draw_Pressed(object sender, EventArgs e)
        {
            Page drawpage = new FingerPaintPage(imageAsBytes);
            await Navigation.PushAsync(drawpage);
        }
    }

}
