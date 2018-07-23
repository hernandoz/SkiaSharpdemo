using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Xamarin.Forms;

using SkiaSharp;
using SkiaSharp.Views.Forms;

using TouchTracking;
using System.IO;

namespace TouchTrackingEffectDemos
{
    public partial class FingerPaintPage : ContentPage
    {
        Dictionary<long, FingerPaintPolyline> inProgressPolylines = new Dictionary<long, FingerPaintPolyline>();
        List<FingerPaintPolyline> completedPolylines = new List<FingerPaintPolyline>();

        SKBitmap resourceBitmap;
        SKImage screenImage;
        byte[] imageAsBytes;

        SKPaint paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

        public FingerPaintPage(byte[] image)
        {
            InitializeComponent();

            resourceBitmap = SKBitmap.Decode(image);
        }

        void OnTouchEffectAction(object sender, TouchActionEventArgs args)
        {
            switch (args.Type)
            {
                case TouchActionType.Pressed:
                    if (!inProgressPolylines.ContainsKey(args.Id))
                    {
                        Color strokeColor = (Color)typeof(Color).GetRuntimeField(colorPicker.Items[colorPicker.SelectedIndex]).GetValue(null);
                        float strokeWidth = ConvertToPixel(new float[] { 1, 2, 5, 10, 20 }[widthPicker.SelectedIndex]);

                        FingerPaintPolyline polyline = new FingerPaintPolyline
                        {
                            StrokeColor = strokeColor,
                            StrokeWidth = strokeWidth
                        };
                        polyline.Path.MoveTo(ConvertToPixel(args.Location));

                        inProgressPolylines.Add(args.Id, polyline);
                        canvasView.InvalidateSurface();
                    }
                    break;

                case TouchActionType.Moved:
                    if (inProgressPolylines.ContainsKey(args.Id))
                    {
                        FingerPaintPolyline polyline = inProgressPolylines[args.Id];
                        polyline.Path.LineTo(ConvertToPixel(args.Location));
                        canvasView.InvalidateSurface();
                    }
                    break;

                case TouchActionType.Released:
                    if (inProgressPolylines.ContainsKey(args.Id))
                    {
                        completedPolylines.Add(inProgressPolylines[args.Id]);
                        inProgressPolylines.Remove(args.Id);
                        canvasView.InvalidateSurface();
                    }
                    break;

                case TouchActionType.Cancelled:
                    if (inProgressPolylines.ContainsKey(args.Id))
                    {
                        inProgressPolylines.Remove(args.Id);
                        canvasView.InvalidateSurface();
                    }
                    break;
            }
        }

        void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs args)
        {
            SKCanvas canvas = args.Surface.Canvas;
            canvas.Clear();

            canvas.DrawBitmap(resourceBitmap, 0, 0);

            foreach (FingerPaintPolyline polyline in completedPolylines)
            {
                paint.Color = polyline.StrokeColor.ToSKColor();
                paint.StrokeWidth = polyline.StrokeWidth;
                canvas.DrawPath(polyline.Path, paint);
            }

            foreach (FingerPaintPolyline polyline in inProgressPolylines.Values)
            {
                paint.Color = polyline.StrokeColor.ToSKColor();
                paint.StrokeWidth = polyline.StrokeWidth;
                canvas.DrawPath(polyline.Path, paint);
            }
        }

        SKPoint ConvertToPixel(Point pt)
        {
            return new SKPoint((float)(canvasView.CanvasSize.Width * pt.X / canvasView.Width),
                               (float)(canvasView.CanvasSize.Height * pt.Y / canvasView.Height));
        }

        float ConvertToPixel(float fl)
        {
            return (float)(canvasView.CanvasSize.Width * fl / canvasView.Width);
        }

        async void OnClearButtonClicked(object sender, EventArgs args)
        {
            //completedPolylines.Clear(); 
            //canvasView.InvalidateSurface();

            var surface = SKSurface.Create(
            resourceBitmap.Width,
            resourceBitmap.Height,
            SKImageInfo.PlatformColorType,
            SKAlphaType.Premul);

            var canvas = surface.Canvas;
            canvas.Clear();

            canvas.DrawBitmap(resourceBitmap, 0, 0);

            foreach (FingerPaintPolyline path in completedPolylines)
                canvas.DrawPath(path.Path, paint);

            foreach (FingerPaintPolyline path in inProgressPolylines.Values)
                canvas.DrawPath(path.Path, paint);

            canvas.Flush();

            var skImage = surface.Snapshot();

            using (var data = skImage.Encode(SKEncodedImageFormat.Jpeg, 100))
            {
                Image _image = new Image();
                _image.Source = ImageSource.FromStream(() => new MemoryStream(imageAsBytes));
            }

            MessagingCenter.Send<FingerPaintPage, byte[]>(this, "saveimage", imageAsBytes);
            await Navigation.PopAsync();
        }
    }
}