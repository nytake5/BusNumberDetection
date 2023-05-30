using Java.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BusNumberDetection
{
    public partial class MainPage : ContentPage
    {
        private static HttpClient HttpClient = new HttpClient();
        Button takePhotoBtn;
        Button getPhotoBtn;
        Image img;
        public MainPage()
        {
            InitializeComponent();
            img = new Image();
            takePhotoBtn = new Button { Text = "Сделать фото" };

            getPhotoBtn = new Button { Text = "Выбрать фото" };
            takePhotoBtn.Clicked += TakePhotoAsync;
            getPhotoBtn.Clicked += GetPhotoAsync;

            Content = new StackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                Children = {
                    new StackLayout
                    {
                         Children = {takePhotoBtn, getPhotoBtn},
                         Orientation = StackOrientation.Horizontal,
                         HorizontalOptions = LayoutOptions.CenterAndExpand
                    },
                    img
                }
            };
        }

        async void GetPhotoAsync(object sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.PickPhotoAsync();
                img.Source = ImageSource.FromFile(photo.FullPath);

                await SendPhotoAsyncAndProcessResponse(photo.FullPath, photo.FileName);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Сообщение об ошибке", ex.Message, "OK");
            }
        }


        private async void TakePhotoAsync(object sender, EventArgs e)
        {
            var photo = await MediaPicker.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = $"xamarin.{DateTime.Now.ToString("dd.MM.yyyy_hh.mm.ss")}.png"
            });

            var newFile = Path.Combine(FileSystem.AppDataDirectory, photo.FileName);
            using (var stream = await photo.OpenReadAsync())
            using (var newStream = System.IO.File.OpenWrite(newFile))
                await stream.CopyToAsync(newStream);
            img.Source = ImageSource.FromFile(photo.FullPath);

            await SendPhotoAsyncAndProcessResponse(photo.FullPath, photo.FileName);
        }

        private async Task SendPhotoAsyncAndProcessResponse(string photoFullPath, string title)
        {
            try
            {
                MultipartFormDataContent content = new MultipartFormDataContent();
                var bytes = GetBytesFromImage(photoFullPath);
                ByteArrayContent byteArrayContent = new ByteArrayContent(bytes);
                content.Add(byteArrayContent, "file", title);
                var response = await HttpClient.PostAsync("http://localhost:5000/predict", content);
                var responseStr = response.Content.ReadAsStringAsync().Result;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    await DisplayAlert("Номер Автобуса", responseStr, "ОK");
                }
                else
                {
                    await DisplayAlert("Что-то пошло не так", responseStr, "ОK");
                }
            }
            catch (Exception)
            {
                await DisplayAlert("Что-то пошло не так", "Попробуйте ещё раз!", "ОK");
            }
        }

        private static byte[] GetBytesFromImage(string imagePath)
        {
            var imgFile = new Java.IO.File(imagePath);
            var stream = new FileInputStream(imgFile);
            var bytes = new byte[imgFile.Length()];
            stream.Read(bytes);
            return bytes;
        }
    }
}
