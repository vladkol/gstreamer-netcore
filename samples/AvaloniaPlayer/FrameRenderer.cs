
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace GStreamerPlayer
{
    public class GstFrameRenderer : Control
    {
        private readonly Stretch Stretch = Stretch.Uniform;
        private WriteableBitmap Bitmap;
        private readonly SolidColorBrush BackgroundBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
        }
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            Bitmap?.Dispose();
            Bitmap = null;
            base.OnDetachedFromLogicalTree(e);
        }

        public override void Render(DrawingContext context)
        {
            Rect rect = new Rect(0, 0, this.Bounds.Width, this.Bounds.Height);

            context.FillRectangle(BackgroundBrush, rect);
            var bitmap = Bitmap;

            if (bitmap != null 
                && bitmap.Size.Width > 0 && bitmap.Size.Height > 0
                && rect.Width > 0 && rect.Height > 0)
            {
                var bmpSize = bitmap.Size;
                Size size = Stretch.CalculateSize(rect.Size, bmpSize);
                
                Rect drawRect = new Rect((rect.Width - size.Width)/2, 
                                         (rect.Height - size.Height)/2, 
                                         size.Width, size.Height);

                context.DrawImage(bitmap, 1, new Rect(0, 0, bmpSize.Width, bmpSize.Height), drawRect);
            }
            base.Render(context);
        }

        public void Clear()
        {
            Bitmap = null;
            Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
        }

        public void UpdateImage(ref Gst.MapInfo map, int width, int height)
        {
            if (Bitmap == null || Bitmap.PixelSize.Width != width || Bitmap.PixelSize.Height != height)
            {
                var oldBitmap = Bitmap;
                Bitmap = new Avalonia.Media.Imaging.WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), Avalonia.Platform.PixelFormat.Rgba8888);
                oldBitmap?.Dispose();
            }

            using (var l = Bitmap.Lock())
            {
                map.CopyTo(l.Address, l.RowBytes * l.Size.Height);
            }
            VisualRoot?.Renderer.AddDirty(this);
        }

    }
}