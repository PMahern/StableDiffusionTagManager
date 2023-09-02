/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
// Port from: https://github.com/cyotek/Cyotek.Windows.Forms.ImageBox to AvaloniaUI
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static UVtools.AvaloniaControls.AdvancedImageBox;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Color = Avalonia.Media.Color;
using Image = Avalonia.Controls.Image;
using Pen = Avalonia.Media.Pen;
using Point = Avalonia.Point;
using Size = Avalonia.Size;
using StableDiffusionTagManager.Extensions;

namespace UVtools.AvaloniaControls
{
    public partial class AdvancedImageBox : UserControl
    {
        #region Bindable Base
        /// <summary>
        ///     Multicast event for property change notifications.
        /// </summary>
        private PropertyChangedEventHandler? _propertyChanged;

        public new event PropertyChangedEventHandler PropertyChanged
        {
            add => _propertyChanged += value;
            remove => _propertyChanged -= value;
        }
        protected bool RaiseAndSetIfChanged<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
        }

        /// <summary>
        ///     Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="propertyName">
        ///     Name of the property used to notify listeners.  This
        ///     value is optional and can be provided automatically when invoked from compilers
        ///     that support <see cref="CallerMemberNameAttribute" />.
        /// </param>
        protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            var e = new PropertyChangedEventArgs(propertyName);
            OnPropertyChanged(e);
            _propertyChanged?.Invoke(this, e);
        }
        #endregion

        #region Sub Classes

        /// <summary>
        /// Represents available levels of zoom in an <see cref="AdvancedImageBox"/> control
        /// </summary>
        public class ZoomLevelCollection : IList<int>
        {
            #region Public Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="ZoomLevelCollection"/> class.
            /// </summary>
            public ZoomLevelCollection()
            {
                List = new SortedList<int, int>();
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ZoomLevelCollection"/> class.
            /// </summary>
            /// <param name="collection">The default values to populate the collection with.</param>
            /// <exception cref="System.ArgumentNullException">Thrown if the <c>collection</c> parameter is null</exception>
            public ZoomLevelCollection(IEnumerable<int> collection)
                : this()
            {
                if (collection == null)
                {
                    throw new ArgumentNullException(nameof(collection));
                }

                AddRange(collection);
            }

            #endregion

            #region Public Class Properties

            /// <summary>
            /// Returns the default zoom levels
            /// </summary>
            public static ZoomLevelCollection Default =>
                new(new[] {
                7, 10, 15, 20, 25, 30, 50, 70, 100, 150, 200, 300, 400, 500, 600, 700, 800, 1200, 1600, 3200, 6400
                });

            #endregion

            #region Public Properties

            /// <summary>
            /// Gets the number of elements contained in the <see cref="ZoomLevelCollection" />.
            /// </summary>
            /// <returns>
            /// The number of elements contained in the <see cref="ZoomLevelCollection" />.
            /// </returns>
            public int Count => List.Count;

            /// <summary>
            /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
            /// </summary>
            /// <value><c>true</c> if this instance is read only; otherwise, <c>false</c>.</value>
            /// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only; otherwise, false.
            /// </returns>
            public bool IsReadOnly => false;

            /// <summary>
            /// Gets or sets the zoom level at the specified index.
            /// </summary>
            /// <param name="index">The index.</param>
            public int this[int index]
            {
                get => List.Values[index];
                set
                {
                    List.RemoveAt(index);
                    Add(value);
                }
            }

            #endregion

            #region Protected Properties

            /// <summary>
            /// Gets or sets the backing list.
            /// </summary>
            protected SortedList<int, int> List { get; set; }

            #endregion

            #region Public Members

            /// <summary>
            /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.
            /// </summary>
            /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
            public void Add(int item)
            {
                List.Add(item, item);
            }

            /// <summary>
            /// Adds a range of items to the <see cref="ZoomLevelCollection"/>.
            /// </summary>
            /// <param name="collection">The items to add to the collection.</param>
            /// <exception cref="System.ArgumentNullException">Thrown if the <c>collection</c> parameter is null.</exception>
            public void AddRange(IEnumerable<int> collection)
            {
                if (collection == null)
                {
                    throw new ArgumentNullException(nameof(collection));
                }

                foreach (int value in collection)
                {
                    Add(value);
                }
            }

            /// <summary>
            /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" />.
            /// </summary>
            public void Clear()
            {
                List.Clear();
            }

            /// <summary>
            /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
            /// </summary>
            /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
            /// <returns>true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false.</returns>
            public bool Contains(int item)
            {
                return List.ContainsKey(item);
            }

            /// <summary>
            /// Copies a range of elements this collection into a destination <see cref="Array"/>.
            /// </summary>
            /// <param name="array">The <see cref="Array"/> that receives the data.</param>
            /// <param name="arrayIndex">A 64-bit integer that represents the index in the <see cref="Array"/> at which storing begins.</param>
            public void CopyTo(int[] array, int arrayIndex)
            {
                for (int i = 0; i < Count; i++)
                {
                    array[arrayIndex + i] = List.Values[i];
                }
            }

            /// <summary>
            /// Finds the index of a zoom level matching or nearest to the specified value.
            /// </summary>
            /// <param name="zoomLevel">The zoom level.</param>
            public int FindNearest(int zoomLevel)
            {
                int nearestValue = List.Values[0];
                int nearestDifference = Math.Abs(nearestValue - zoomLevel);
                for (int i = 1; i < Count; i++)
                {
                    int value = List.Values[i];
                    int difference = Math.Abs(value - zoomLevel);
                    if (difference < nearestDifference)
                    {
                        nearestValue = value;
                        nearestDifference = difference;
                    }
                }
                return nearestValue;
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
            public IEnumerator<int> GetEnumerator()
            {
                return List.Values.GetEnumerator();
            }

            /// <summary>
            /// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1" />.
            /// </summary>
            /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1" />.</param>
            /// <returns>The index of <paramref name="item" /> if found in the list; otherwise, -1.</returns>
            public int IndexOf(int item)
            {
                return List.IndexOfKey(item);
            }

            /// <summary>
            /// Not implemented.
            /// </summary>
            /// <param name="index">The index.</param>
            /// <param name="item">The item.</param>
            /// <exception cref="System.NotImplementedException">Not implemented</exception>
            public void Insert(int index, int item)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Returns the next increased zoom level for the given current zoom.
            /// </summary>
            /// <param name="zoomLevel">The current zoom level.</param>
            /// <param name="constrainZoomLevel">When positive, constrain maximum zoom to this value</param>
            /// <returns>The next matching increased zoom level for the given current zoom if applicable, otherwise the nearest zoom.</returns>
            public int NextZoom(int zoomLevel, int constrainZoomLevel = 0)
            {
                var index = IndexOf(FindNearest(zoomLevel));
                if (index < Count - 1) index++;

                return constrainZoomLevel > 0 && this[index] >= constrainZoomLevel ? constrainZoomLevel : this[index];
            }

            /// <summary>
            /// Returns the next decreased zoom level for the given current zoom.
            /// </summary>
            /// <param name="zoomLevel">The current zoom level.</param>
            /// <param name="constrainZoomLevel">When positive, constrain minimum zoom to this value</param>
            /// <returns>The next matching decreased zoom level for the given current zoom if applicable, otherwise the nearest zoom.</returns>
            public int PreviousZoom(int zoomLevel, int constrainZoomLevel = 0)
            {
                var index = IndexOf(FindNearest(zoomLevel));
                if (index > 0) index--;

                return constrainZoomLevel > 0 && this[index] <= constrainZoomLevel ? constrainZoomLevel : this[index];
            }

            /// <summary>
            /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.
            /// </summary>
            /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
            /// <returns>true if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
            public bool Remove(int item)
            {
                return List.Remove(item);
            }

            /// <summary>
            /// Removes the element at the specified index of the <see cref="ZoomLevelCollection"/>.
            /// </summary>
            /// <param name="index">The zero-based index of the element to remove.</param>
            public void RemoveAt(int index)
            {
                List.RemoveAt(index);
            }

            /// <summary>
            /// Copies the elements of the <see cref="ZoomLevelCollection"/> to a new array.
            /// </summary>
            /// <returns>An array containing copies of the elements of the <see cref="ZoomLevelCollection"/>.</returns>
            public int[] ToArray()
            {
                var results = new int[Count];
                CopyTo(results, 0);

                return results;
            }

            #endregion

            #region IList<int> Members

            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>An <see cref="ZoomLevelCollection" /> object that can be used to iterate through the collection.</returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion
        }

        #endregion

        #region Enums
        enum CursorsTypes : int
        {
            Default = 0,
            Panning = 1,
            SelectionMode = 2,
            DragSelection = 3,
            ResizeTopLeft = 4,
            ResizeTopRight = 5,
            PaintMode = 6
        }

        static Cursor[] Cursors { get; } =  {
                Cursor.Default,
                new Cursor(StandardCursorType.SizeAll),
                new Cursor(StandardCursorType.Cross),
                new Cursor(StandardCursorType.DragMove),
                new Cursor(StandardCursorType.TopLeftCorner),
                new Cursor(StandardCursorType.TopRightCorner),
                new Cursor(StandardCursorType.Cross)
            };

        /// <summary>
        /// Determines the sizing mode of an image hosted in an <see cref="AdvancedImageBox" /> control.
        /// </summary>
        public enum SizeModes : byte
        {
            /// <summary>
            /// The image is displayed according to current zoom and scroll properties.
            /// </summary>
            Normal,

            /// <summary>
            /// The image is stretched to fill the client area of the control.
            /// </summary>
            Stretch,

            /// <summary>
            /// The image is stretched to fill as much of the client area of the control as possible, whilst retaining the same aspect ratio for the width and height.
            /// </summary>
            Fit
        }

        [Flags]
        public enum MouseButtons : byte
        {
            None = 0,
            LeftButton = 1,
            MiddleButton = 2,
            RightButton = 4
        }

        /// <summary>
        /// Describes the zoom action occurring
        /// </summary>
        [Flags]
        public enum ZoomActions : byte
        {
            /// <summary>
            /// No action.
            /// </summary>
            None = 0,

            /// <summary>
            /// The control is increasing the zoom.
            /// </summary>
            ZoomIn = 1,

            /// <summary>
            /// The control is decreasing the zoom.
            /// </summary>
            ZoomOut = 2,

            /// <summary>
            /// The control zoom was reset.
            /// </summary>
            ActualSize = 4
        }

        public enum SelectionModes
        {
            /// <summary>
            ///   No selection.
            /// </summary>
            None,

            /// <summary>
            ///   Rectangle selection.
            /// </summary>
            Rectangle,

            /// <summary>
            ///   Zoom selection.
            /// </summary>
            Zoom
        }

        #endregion

        #region UI Controls

        public Vector Offset
        {
            get => new(HorizontalScrollBar.Value, VerticalScrollBar.Value);
            set
            {
                HorizontalScrollBar.Value = value.X;
                VerticalScrollBar.Value = value.Y;
                RaisePropertyChanged();
                TriggerRender();
            }
        }

        public enum DraggingMode : byte
        {
            None = 0,
            Full = 1,
            TopLeft = 2,
            TopRight = 3,
            BottomLeft = 4,
            BottomRight = 5
        }

        private bool IsDragging()
        {
            return _currentDraggingMode != DraggingMode.None;
        }
        public Size ViewPortSize => ViewPort.Bounds.Size;
        #endregion

        #region Private Members
        private Point _startMousePosition;
        private Point _dragRelativePosition;
        private Vector _startScrollPosition;
        private bool _isPainting;
        private bool _isMasking;
        private bool _isPanning;
        private DraggingMode _currentDraggingMode;
        private bool _isSelecting;
        private Bitmap? _trackerImage;
        private bool _canRender = true;
        private Point _pointerPosition;
        #endregion

        #region Properties
        public static readonly DirectProperty<AdvancedImageBox, bool> CanRenderProperty =
            AvaloniaProperty.RegisterDirect<AdvancedImageBox, bool>(
                nameof(CanRender),
                o => o.CanRender);

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public bool CanRender
        {
            get => _canRender;
            set
            {
                if (!SetAndRaise(CanRenderProperty, ref _canRender, value)) return;
                if (_canRender) TriggerRender();
            }
        }

        public static readonly StyledProperty<byte> GridCellSizeProperty =
            AvaloniaProperty.Register<AdvancedImageBox, byte>(nameof(GridCellSize), 15);

        /// <summary>
        /// Gets or sets the grid cell size
        /// </summary>
        public byte GridCellSize
        {
            get => GetValue(GridCellSizeProperty);
            set => SetValue(GridCellSizeProperty, value);
        }

        public static readonly StyledProperty<ISolidColorBrush> GridColorProperty =
            AvaloniaProperty.Register<AdvancedImageBox, ISolidColorBrush>(nameof(GridColor), Avalonia.Media.Brushes.Gainsboro);

        /// <summary>
        /// Gets or sets the color used to create the checkerboard style background
        /// </summary>
        public ISolidColorBrush GridColor
        {
            get => GetValue(GridColorProperty);
            set => SetValue(GridColorProperty, value);
        }

        public static readonly StyledProperty<ISolidColorBrush> GridColorAlternateProperty =
            AvaloniaProperty.Register<AdvancedImageBox, ISolidColorBrush>(nameof(GridColorAlternate), Avalonia.Media.Brushes.White);

        /// <summary>
        /// Gets or sets the color used to create the checkerboard style background
        /// </summary>
        public ISolidColorBrush GridColorAlternate
        {
            get => GetValue(GridColorAlternateProperty);
            set => SetValue(GridColorAlternateProperty, value);
        }

        private static void ImageChanged(AdvancedImageBox control, bool before)
        {
            if (!before)
            {
                var value = control.GetValue(ImageProperty);
                control.mipScaleFactor = 0;
                control.SelectNone();
                control.IsPainted = false;
                if (value != null && !control.paintLayers.ContainsKey(value))
                {
                    if (!control.paintLayers.ContainsKey(value))
                    {
                        control.paintLayers[value] = new List<RenderTargetBitmap>();
                    }
                    else
                    {
                        control.IsPainted = control.paintLayers[value].Any();
                    }
                }

                if (value != null && !control.maskLayers.ContainsKey(value))
                {
                    control.maskLayers[value] = new List<RenderTargetBitmap>();
                }

                control.ZoomToFit();
                control.UpdateViewPort();
                control.TriggerRender();
                control.RaisePropertyChanged(nameof(IsImageLoaded));
            }
        }

        private Dictionary<(Bitmap, int), (Bitmap image, List<RenderTargetBitmap> paintLayers, List<RenderTargetBitmap> maskLayers)> mips = new();
        private int mipScaleFactor = 0;

        private RenderTargetBitmap CreateBitmapMip(Bitmap source, int width, int height)
        {
            var renderImage = new RenderTargetBitmap(new PixelSize(width, height));
            using (var bitmapRenderContext = renderImage.CreateDrawingContext(null))
            {
                var dc = new DrawingContext(bitmapRenderContext);
                dc.DrawImage(source, new Rect(0, 0, source.PixelSize.Width, source.PixelSize.Height), new Rect(0, 0, width, height), Avalonia.Visuals.Media.Imaging.BitmapInterpolationMode.HighQuality);
            }
            return renderImage;
        }

        private int maxMipReached = 1;
        private void UpdateRenderedMipLevel()
        {
            var image = Image;
            if (image == null)
            {
                mipScaleFactor = 0;
                return;
            }

            var zoomFactor = ZoomFactor;
            var curHeight = Image.PixelSize.Height;
            var curWidth = Image.PixelSize.Width;
            var nextHeight = curHeight / 2;
            var nextWidth = curWidth / 2;
            var minHeight = Image.PixelSize.Height * zoomFactor;
            var minWidth = Image.PixelSize.Width * zoomFactor;
            var newmipScaleFactor = 1;

            while (nextHeight > minHeight && nextWidth > minWidth)
            {
                newmipScaleFactor *= 2;
                curHeight = nextHeight;
                curWidth = nextWidth;
                nextHeight /= 2;
                nextWidth /= 2;
            }

            if (newmipScaleFactor > 1 && newmipScaleFactor != mipScaleFactor)
            {
                var key = (Image, newmipScaleFactor);
                var paintLayers = this.paintLayers[image];
                var maskLayers = this.maskLayers[image];
                if (!mips.ContainsKey(key))
                {
                    var imagemip = CreateBitmapMip(image, curWidth, curHeight);
                    var layerMips = new List<RenderTargetBitmap>();

                    foreach (var layer in paintLayers)
                    {
                        layerMips.Add(CreateBitmapMip(layer, curWidth, curHeight));
                    }

                    var maskLayerMips = new List<RenderTargetBitmap>();
                    foreach (var layer in maskLayers)
                    {
                        layerMips.Add(CreateBitmapMip(layer, curWidth, curHeight));
                    }

                    mips[key] = (image: imagemip, paintLayers: layerMips, maskLayers: maskLayerMips);
                }
                else
                {
                    var entry = mips[key];

                    if (entry.paintLayers.Count() < paintLayers.Count())
                    {
                        var index = entry.paintLayers.Count();
                        for (int i = index; i < paintLayers.Count(); i++)
                        {
                            entry.paintLayers.Add(CreateBitmapMip(paintLayers[i], curWidth, curHeight));
                        }
                    }

                    if (entry.maskLayers.Count() < maskLayers.Count())
                    {
                        var index = entry.maskLayers.Count();
                        for (int i = index; i < maskLayers.Count(); i++)
                        {
                            entry.maskLayers.Add(CreateBitmapMip(maskLayers[i], curWidth, curHeight));
                        }
                    }
                }
            }

            mipScaleFactor = newmipScaleFactor;
            maxMipReached = Math.Max(maxMipReached, mipScaleFactor);
        }

        public List<RenderTargetBitmap> GetImageLayers(Bitmap image)
        {
            if (paintLayers.ContainsKey(image))
            {
                return paintLayers[image];
            }

            return new();
        }

        public List<RenderTargetBitmap> GetImageMaskLayers(Bitmap image)
        {
            if (maskLayers.ContainsKey(image))
            {
                return maskLayers[image];
            }

            return new();

        }
        public void ClearMipsAndLayers(Bitmap image)
        {
            if (paintLayers.ContainsKey(image))
            {
                paintLayers[image].Clear();
                for (int i = 2; i < maxMipReached; i++)
                {
                    mips.Remove((image, i));
                }
            }

            if (maskLayers.ContainsKey(image))
            {
                maskLayers[image].Clear();
                for (int i = 2; i < maxMipReached; i++)
                {
                    mips.Remove((image, i));
                }
            }
        }

        public void UndoLastPaint(Bitmap image)
        {
            if (paintLayers.ContainsKey(image) && paintLayers[image].Any())
            {
                var set = paintLayers[image];
                set.Remove(set.Last());
                for (int i = 2; i <= maxMipReached; i *= 2)
                {
                    var key = (image, i);
                    if (mips.ContainsKey(key))
                    {
                        var paintLayerMips = mips[key].paintLayers;
                        while (paintLayerMips.Count() > set.Count())
                        {
                            paintLayerMips.Remove(paintLayerMips.Last());
                        }
                    }
                }
                IsPainted = set.Any();
                TriggerRender();
            }
            else
            {
                IsPainted = false;
            }
        }

        public void UndoLastMask(Bitmap image)
        {
            if (maskLayers.ContainsKey(image) && maskLayers[image].Any())
            {
                var set = maskLayers[image];
                set.Remove(set.Last());
                for (int i = 2; i <= maxMipReached; i *= 2)
                {
                    var key = (image, i);
                    if (mips.ContainsKey(key))
                    {
                        var maskLayerMips = mips[key].maskLayers;
                        while (maskLayerMips.Count() > set.Count())
                        {
                            maskLayerMips.Remove(maskLayerMips.Last());
                        }
                    }
                }
                TriggerRender();
            }
        }

        public static readonly StyledProperty<Bitmap?> ImageProperty =
            AvaloniaProperty.Register<AdvancedImageBox, Bitmap?>(nameof(Image), notifying: (obj, before) => ImageChanged((AdvancedImageBox)obj, before));

        /// <summary>
        /// Gets or sets the image to be displayed
        /// </summary>
        public Bitmap? Image
        {
            get => GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }

        public WriteableBitmap? ImageAsWriteableBitmap
        {
            get
            {
                if (Image is null) return null;
                return (WriteableBitmap)Image;
            }
        }

        public bool ImageHasPaint(Bitmap image)
        {
            return paintLayers.ContainsKey(image) && paintLayers[image].Any();
        }

        public bool IsImageLoaded => Image is not null;

        public static readonly DirectProperty<AdvancedImageBox, Bitmap?> TrackerImageProperty =
            AvaloniaProperty.RegisterDirect<AdvancedImageBox, Bitmap?>(
                nameof(TrackerImage),
                o => o.TrackerImage,
                (o, v) => o.TrackerImage = v);

        /// <summary>
        /// Gets or sets an image to follow the mouse pointer
        /// </summary>
        public Bitmap? TrackerImage
        {
            get => _trackerImage;
            set
            {
                if (!SetAndRaise(TrackerImageProperty, ref _trackerImage, value)) return;
                TriggerRender();
                RaisePropertyChanged(nameof(HaveTrackerImage));
            }
        }

        public bool HaveTrackerImage => _trackerImage is not null;

        public static readonly StyledProperty<bool> TrackerImageAutoZoomProperty =
            AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(TrackerImageAutoZoom), true);

        /// <summary>
        /// Gets or sets if the tracker image will be scaled to the current zoom
        /// </summary>
        public bool TrackerImageAutoZoom
        {
            get => GetValue(TrackerImageAutoZoomProperty);
            set => SetValue(TrackerImageAutoZoomProperty, value);
        }

        public bool IsHorizontalBarVisible
        {
            get
            {
                if (Image is null) return false;
                if (SizeMode != SizeModes.Normal) return false;
                return ScaledImageWidth > ViewPortSize.Width;
            }
        }

        public bool IsVerticalBarVisible
        {
            get
            {
                if (Image is null) return false;
                if (SizeMode != SizeModes.Normal) return false;
                return ScaledImageHeight > ViewPortSize.Height;
            }
        }

        public static readonly StyledProperty<bool> ShowGridProperty =
            AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(ShowGrid), true);

        /// <summary>
        /// Gets or sets the grid visibility when reach high zoom levels
        /// </summary>
        public bool ShowGrid
        {
            get => GetValue(ShowGridProperty);
            set => SetValue(ShowGridProperty, value);
        }

        public static readonly DirectProperty<AdvancedImageBox, Point> PointerPositionProperty =
            AvaloniaProperty.RegisterDirect<AdvancedImageBox, Point>(
                nameof(PointerPosition),
                o => o.PointerPosition);

        /// <summary>
        /// Gets the current pointer position
        /// </summary>
        public Point PointerPosition
        {
            get => _pointerPosition;
            private set => SetAndRaise(PointerPositionProperty, ref _pointerPosition, value);
        }

        public static readonly DirectProperty<AdvancedImageBox, bool> IsPanningProperty =
            AvaloniaProperty.RegisterDirect<AdvancedImageBox, bool>(
                nameof(IsPanning),
                o => o.IsPanning);

        /// <summary>
        /// Gets if control is currently panning
        /// </summary>
        public bool IsPanning
        {
            get => _isPanning;
            protected set
            {
                if (!SetAndRaise(IsPanningProperty, ref _isPanning, value)) return;
                _startScrollPosition = Offset;
                UpdateCursor();
            }
        }

        public static readonly DirectProperty<AdvancedImageBox, bool> IsSelectingProperty =
            AvaloniaProperty.RegisterDirect<AdvancedImageBox, bool>(
                nameof(IsSelecting),
                o => o.IsSelecting);

        /// <summary>
        /// Gets if control is currently selecting a ROI
        /// </summary>
        public bool IsSelecting
        {
            get => _isSelecting;
            protected set => SetAndRaise(IsSelectingProperty, ref _isSelecting, value);
        }

        public static readonly DirectProperty<AdvancedImageBox, DraggingMode> CurrentDraggingModeProperty =
            AvaloniaProperty.RegisterDirect<AdvancedImageBox, DraggingMode>(
                nameof(CurrentDraggingMode),
                o => o.CurrentDraggingMode);

        /// <summary>
        /// Gets if control is currently dragging a ROI
        /// </summary>
        public DraggingMode CurrentDraggingMode
        {
            get => _currentDraggingMode;
            protected set
            {
                if (!SetAndRaise(CurrentDraggingModeProperty, ref _currentDraggingMode, value)) return;
                RaisePropertyChanged(nameof(IsDraggingSelection));
                UpdateCursor();
            }
        }

        /// <summary>
        /// Gets if control is currently dragging a ROI
        /// </summary>
        public bool IsDraggingSelection
        {
            get => _currentDraggingMode != DraggingMode.None;
        }

        public static readonly DirectProperty<AdvancedImageBox, bool> IsPaintingProperty =
            AvaloniaProperty.RegisterDirect<AdvancedImageBox, bool>(
                nameof(IsPainting),
                o => o.IsPainting);

        /// <summary>
        /// Gets if control is currently dragging a ROI
        /// </summary>
        public bool IsPainting
        {
            get => _isPainting;
            protected set => SetAndRaise(IsPaintingProperty, ref _isPainting, value);
        }

        public static readonly DirectProperty<AdvancedImageBox, bool> IsMaskingProperty =
            AvaloniaProperty.RegisterDirect<AdvancedImageBox, bool>(
                nameof(IsMasking),
                o => o.IsMasking);

        /// <summary>
        /// Gets if control is currently dragging a ROI
        /// </summary>
        public bool IsMasking
        {
            get => _isMasking;
            protected set => SetAndRaise(IsPaintingProperty, ref _isMasking, value);
        }

        private static void OnAspectRatioLocked(AdvancedImageBox imageBox, bool before)
        {
            if (!before)
            {
                if (imageBox.IsAspectRatioLocked && !imageBox.SelectionRegion.IsEmpty)
                {
                    imageBox._lockedAspectRatio = imageBox.SelectionRegion.Width / imageBox.SelectionRegion.Height;
                }
            }
        }

        public static readonly StyledProperty<bool> IsAspectRatioLockedProperty =
            AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(IsAspectRatioLocked), false, notifying: (control, before) => OnAspectRatioLocked((AdvancedImageBox)control, before));

        private double _lockedAspectRatio;

        /// <summary>
        /// Gets if a
        /// </summary>
        public bool IsAspectRatioLocked
        {
            get => GetValue(IsAspectRatioLockedProperty);
            set
            {
                SetValue(IsAspectRatioLockedProperty, value);
            }
        }

        /// <summary>
        /// Gets the center point of the viewport
        /// </summary>
        public Point CenterPoint
        {
            get
            {
                var viewport = GetImageViewPort();
                return new(viewport.Width / 2, viewport.Height / 2);
            }
        }

        public static readonly StyledProperty<bool> AutoPanProperty =
            AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(AutoPan), true);

        /// <summary>
        /// Gets or sets if the control can pan with the mouse
        /// </summary>
        public bool AutoPan
        {
            get => GetValue(AutoPanProperty);
            set => SetValue(AutoPanProperty, value);
        }

        public static void TriggerRenderOnMouseButtonChange(IAvaloniaObject obj, bool before)
        {
            if (!before)
            {
                var control = obj as AdvancedImageBox;
                if (control != null)
                {
                    control.TriggerRender();
                }
            }
        }
        public static readonly StyledProperty<MouseButtons> PanWithMouseButtonsProperty =
            AvaloniaProperty.Register<AdvancedImageBox, MouseButtons>(nameof(PanWithMouseButtons), MouseButtons.LeftButton | MouseButtons.MiddleButton | MouseButtons.RightButton, notifying: TriggerRenderOnMouseButtonChange);

        /// <summary>
        /// Gets or sets the mouse buttons to pan the image
        /// </summary>
        public MouseButtons PanWithMouseButtons
        {
            get => GetValue(PanWithMouseButtonsProperty);
            set => SetValue(PanWithMouseButtonsProperty, value);
        }

        public static readonly StyledProperty<bool> PanWithArrowsProperty =
            AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(PanWithArrows), true, notifying: TriggerRenderOnMouseButtonChange);

        /// <summary>
        /// Gets or sets if the control can pan with the keyboard arrows
        /// </summary>
        public bool PanWithArrows
        {
            get => GetValue(PanWithArrowsProperty);
            set => SetValue(PanWithArrowsProperty, value);
        }

        public static readonly StyledProperty<MouseButtons> SelectWithMouseButtonsProperty =
            AvaloniaProperty.Register<AdvancedImageBox, MouseButtons>(nameof(SelectWithMouseButtons), MouseButtons.LeftButton | MouseButtons.RightButton, notifying: TriggerRenderOnMouseButtonChange);


        /// <summary>
        /// Gets or sets the mouse buttons to select a region on image
        /// </summary>
        public MouseButtons SelectWithMouseButtons
        {
            get => GetValue(SelectWithMouseButtonsProperty);
            set => SetValue(SelectWithMouseButtonsProperty, value);
        }

        public static readonly StyledProperty<MouseButtons> PaintWithMouseButtonsProperty =
            AvaloniaProperty.Register<AdvancedImageBox, MouseButtons>(nameof(PaintWithMouseButtons), MouseButtons.MiddleButton, notifying: TriggerRenderOnMouseButtonChange);

        /// <summary>
        /// Gets or sets the mouse buttons to paint on the image
        /// </summary>
        public MouseButtons PaintWithMouseButtons
        {
            get => GetValue(PaintWithMouseButtonsProperty);
            set => SetValue(PaintWithMouseButtonsProperty, value);
        }

        public static readonly StyledProperty<MouseButtons> EyeDropWithMouseButtonsProperty =
            AvaloniaProperty.Register<AdvancedImageBox, MouseButtons>(nameof(EyeDropWithMouseButtons), MouseButtons.None, notifying: TriggerRenderOnMouseButtonChange);

        /// <summary>
        /// Gets or sets the mouse buttons to EyeDrop on the image
        /// </summary>
        public MouseButtons EyeDropWithMouseButtons
        {
            get => GetValue(EyeDropWithMouseButtonsProperty);
            set => SetValue(EyeDropWithMouseButtonsProperty, value);
        }

        public static readonly StyledProperty<MouseButtons> MaskWithMouseButtonsProperty =
            AvaloniaProperty.Register<AdvancedImageBox, MouseButtons>(nameof(MaskWithMouseButtons), MouseButtons.None, notifying: TriggerRenderOnMouseButtonChange);

        /// <summary>
        /// Gets or sets the mouse buttons to paint on the image
        /// </summary>
        public MouseButtons MaskWithMouseButtons
        {
            get => GetValue(MaskWithMouseButtonsProperty);
            set => SetValue(MaskWithMouseButtonsProperty, value);
        }

        public static readonly StyledProperty<int> PaintBrushSizeProperty =
            AvaloniaProperty.Register<AdvancedImageBox, int>(nameof(PaintBrushSize), 5);

        /// <summary>
        /// Gets or sets size of the paint brush
        /// </summary>
        public int PaintBrushSize
        {
            get => GetValue(PaintBrushSizeProperty);
            set => SetValue(PaintBrushSizeProperty, value);
        }

        public static readonly StyledProperty<Color> PaintBrushColorProperty =
            AvaloniaProperty.Register<AdvancedImageBox, Color>(nameof(PaintBrushSize), new Color(255, 255, 255, 255));

        /// <summary>
        /// Gets or sets size of the paint brush
        /// </summary>
        public Color PaintBrushColor
        {
            get => GetValue(PaintBrushColorProperty);
            set => SetValue(PaintBrushColorProperty, value);
        }

        public static readonly StyledProperty<int> MaskBrushSizeProperty =
            AvaloniaProperty.Register<AdvancedImageBox, int>(nameof(MaskBrushSize), 5);

        /// <summary>
        /// Gets or sets size of the mask bursh
        /// </summary>
        public int MaskBrushSize
        {
            get => GetValue(MaskBrushSizeProperty);
            set => SetValue(MaskBrushSizeProperty, value);
        }

        public static readonly StyledProperty<bool> InvertMousePanProperty =
            AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(InvertMousePan), false);

        /// <summary>
        /// Gets or sets if mouse pan is inverted
        /// </summary>
        public bool InvertMousePan
        {
            get => GetValue(InvertMousePanProperty);
            set => SetValue(InvertMousePanProperty, value);
        }

        public static readonly StyledProperty<bool> AutoCenterProperty =
            AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(AutoCenter), true);

        /// <summary>
        /// Gets or sets if image is auto centered
        /// </summary>
        public bool AutoCenter
        {
            get => GetValue(AutoCenterProperty);
            set => SetValue(AutoCenterProperty, value);
        }

        public static readonly StyledProperty<SizeModes> SizeModeProperty =
            AvaloniaProperty.Register<AdvancedImageBox, SizeModes>(nameof(SizeMode), SizeModes.Normal);

        /// <summary>
        /// Gets or sets the image size mode
        /// </summary>
        public SizeModes SizeMode
        {
            get => GetValue(SizeModeProperty);
            set
            {
                SetValue(SizeModeProperty, value);
                SizeModeChanged();
                RaisePropertyChanged(nameof(IsHorizontalBarVisible));
                RaisePropertyChanged(nameof(IsVerticalBarVisible));
            }
        }

        private void SizeModeChanged()
        {
            switch (SizeMode)
            {
                case SizeModes.Normal:
                    HorizontalScrollBar.Visibility = ScrollBarVisibility.Auto;
                    VerticalScrollBar.Visibility = ScrollBarVisibility.Auto;
                    break;
                case SizeModes.Stretch:
                case SizeModes.Fit:
                    HorizontalScrollBar.Visibility = ScrollBarVisibility.Hidden;
                    VerticalScrollBar.Visibility = ScrollBarVisibility.Hidden;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(SizeMode), SizeMode, null);
            }
        }

        public static readonly StyledProperty<bool> AllowZoomProperty =
            AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(AllowZoom), true);

        /// <summary>
        /// Gets or sets if zoom is allowed
        /// </summary>
        public bool AllowZoom
        {
            get => GetValue(AllowZoomProperty);
            set => SetValue(AllowZoomProperty, value);
        }

        public static readonly DirectProperty<AdvancedImageBox, ZoomLevelCollection> ZoomLevelsProperty =
            AvaloniaProperty.RegisterDirect<AdvancedImageBox, ZoomLevelCollection>(
                nameof(ZoomLevels),
                o => o.ZoomLevels,
                (o, v) => o.ZoomLevels = v);

        ZoomLevelCollection _zoomLevels = ZoomLevelCollection.Default;
        /// <summary>
        ///   Gets or sets the zoom levels.
        /// </summary>
        /// <value>The zoom levels.</value>
        public ZoomLevelCollection ZoomLevels
        {
            get => _zoomLevels;
            set => SetAndRaise(ZoomLevelsProperty, ref _zoomLevels, value);
        }

        public static readonly StyledProperty<int> MinZoomProperty =
            AvaloniaProperty.Register<AdvancedImageBox, int>(nameof(MinZoom), 10);

        /// <summary>
        /// Gets or sets the minimum possible zoom.
        /// </summary>
        /// <value>The zoom.</value>
        public int MinZoom
        {
            get => GetValue(MinZoomProperty);
            set => SetValue(MinZoomProperty, value);
        }

        public static readonly StyledProperty<int> MaxZoomProperty =
            AvaloniaProperty.Register<AdvancedImageBox, int>(nameof(MaxZoom), 6400);

        /// <summary>
        /// Gets or sets the maximum possible zoom.
        /// </summary>
        /// <value>The zoom.</value>
        public int MaxZoom
        {
            get => GetValue(MaxZoomProperty);
            set => SetValue(MaxZoomProperty, value);
        }

        public static readonly StyledProperty<bool> ConstrainZoomOutToFitLevelProperty =
            AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(ConstrainZoomOutToFitLevel), true);

        /// <summary>
        /// Gets or sets if the zoom out should constrain to fit image as the lowest zoom level.
        /// </summary>
        public bool ConstrainZoomOutToFitLevel
        {
            get => GetValue(ConstrainZoomOutToFitLevelProperty);
            set => SetValue(ConstrainZoomOutToFitLevelProperty, value);
        }


        public static readonly DirectProperty<AdvancedImageBox, int> OldZoomProperty =
            AvaloniaProperty.RegisterDirect<AdvancedImageBox, int>(
                nameof(OldZoom),
                o => o.OldZoom);

        private int _oldZoom = 100;

        /// <summary>
        /// Gets the previous zoom value
        /// </summary>
        /// <value>The zoom.</value>
        public int OldZoom
        {
            get => _oldZoom;
            private set => SetAndRaise(OldZoomProperty, ref _oldZoom, value);
        }

        public static readonly StyledProperty<int> ZoomProperty =
            AvaloniaProperty.Register<AdvancedImageBox, int>(nameof(Zoom), 100);

        /// <summary>
        ///  Gets or sets the zoom.
        /// </summary>
        /// <value>The zoom.</value>
        public int Zoom
        {
            get => GetValue(ZoomProperty);
            set
            {
                var minZoom = MinZoom;
                if (ConstrainZoomOutToFitLevel) minZoom = Math.Max(ZoomLevelToFit, minZoom);
                var newZoom = Math.Clamp(value, minZoom, MaxZoom);

                var previousZoom = Zoom;
                if (previousZoom == newZoom) return;
                OldZoom = previousZoom;
                SetValue(ZoomProperty, newZoom);

                UpdateViewPort();
                TriggerRender();

                RaisePropertyChanged(nameof(IsHorizontalBarVisible));
                RaisePropertyChanged(nameof(IsVerticalBarVisible));
            }
        }

        /// <summary>
        /// <para>Gets if the image have zoom.</para>
        /// <para>True if zoomed in or out</para>
        /// <para>False if no zoom applied</para>
        /// </summary>
        public bool IsActualSize => Zoom == 100;

        /// <summary>
        /// Gets the zoom factor, the zoom / 100.0
        /// </summary>
        public double ZoomFactor => Zoom / 100.0;

        /// <summary>
        /// Gets the zoom to fit level which shows all the image
        /// </summary>
        public int ZoomLevelToFit
        {
            get
            {
                if (!IsImageLoaded) return 100;
                var image = Image!;

                double zoom;
                double aspectRatio;

                if (image.Size.Width > image.Size.Height)
                {
                    aspectRatio = ViewPortSize.Width / image.Size.Width;
                    zoom = aspectRatio * 100.0;

                    if (ViewPortSize.Height < image.Size.Height * zoom / 100.0)
                    {
                        aspectRatio = ViewPortSize.Height / image.Size.Height;
                        zoom = aspectRatio * 100.0;
                    }
                }
                else
                {
                    aspectRatio = ViewPortSize.Height / image.Size.Height;
                    zoom = aspectRatio * 100.0;

                    if (ViewPortSize.Width < image.Size.Width * zoom / 100.0)
                    {
                        aspectRatio = ViewPortSize.Width / image.Size.Width;
                        zoom = aspectRatio * 100.0;
                    }
                }

                return (int)zoom;
            }
        }

        /// <summary>
        /// Gets the width of the scaled image.
        /// </summary>
        /// <value>The width of the scaled image.</value>
        public double ScaledImageWidth => Image?.Size.Width * ZoomFactor ?? 0;

        /// <summary>
        /// Gets the height of the scaled image.
        /// </summary>
        /// <value>The height of the scaled image.</value>
        public double ScaledImageHeight => Image?.Size.Height * ZoomFactor ?? 0;

        public static readonly DirectProperty<AdvancedImageBox, bool> IsPaintedProperty =
            AvaloniaProperty.RegisterDirect<AdvancedImageBox, bool>(nameof(IsPainted), aib => aib.IsPainted);

        private bool isPainted = false;
        public bool IsPainted
        {
            get => isPainted;
            set
            {
                SetAndRaise(IsPaintedProperty, ref isPainted, value);
            }
        }

        private Dictionary<Bitmap, List<RenderTargetBitmap>> paintLayers = new Dictionary<Bitmap, List<RenderTargetBitmap>>();
        private Dictionary<Bitmap, List<RenderTargetBitmap>> maskLayers = new Dictionary<Bitmap, List<RenderTargetBitmap>>();
        private object framebuffer;
        public static readonly StyledProperty<ISolidColorBrush> PixelGridColorProperty =
            AvaloniaProperty.Register<AdvancedImageBox, ISolidColorBrush>(nameof(PixelGridColor), Avalonia.Media.Brushes.DimGray);

        /// <summary>
        /// Gets or sets the color of the pixel grid.
        /// </summary>
        /// <value>The color of the pixel grid.</value>
        public ISolidColorBrush PixelGridColor
        {
            get => GetValue(PixelGridColorProperty);
            set => SetValue(PixelGridColorProperty, value);
        }

        public static readonly StyledProperty<int> PixelGridZoomThresholdProperty =
            AvaloniaProperty.Register<AdvancedImageBox, int>(nameof(PixelGridZoomThreshold), 5);

        /// <summary>
        /// Gets or sets the minimum size of zoomed pixel's before the pixel grid will be drawn
        /// </summary>
        /// <value>The pixel grid threshold.</value>

        public int PixelGridZoomThreshold
        {
            get => GetValue(PixelGridZoomThresholdProperty);
            set => SetValue(PixelGridZoomThresholdProperty, value);
        }

        public static readonly StyledProperty<SelectionModes> SelectionModeProperty =
            AvaloniaProperty.Register<AdvancedImageBox, SelectionModes>(nameof(SelectionMode), SelectionModes.None);

        public SelectionModes SelectionMode
        {
            get => GetValue(SelectionModeProperty);
            set => SetValue(SelectionModeProperty, value);
        }

        public static readonly StyledProperty<ISolidColorBrush> SelectionColorProperty =
            AvaloniaProperty.Register<AdvancedImageBox, ISolidColorBrush>(nameof(SelectionColor), new SolidColorBrush(new Color(127, 0, 128, 255)));

        public ISolidColorBrush SelectionColor
        {
            get => GetValue(SelectionColorProperty);
            set => SetValue(SelectionColorProperty, value);
        }

        private static void SelectionRegionChanged(AdvancedImageBox control, bool before)
        {
            if (!before)
            {
                control.TriggerRender();
                control.RaisePropertyChanged(nameof(HaveSelection));
                control.RaisePropertyChanged(nameof(SelectionRegionNet));
                control.RaisePropertyChanged(nameof(SelectionPixelSize));
            }
        }

        public static readonly StyledProperty<Rect> SelectionRegionProperty =
            AvaloniaProperty.Register<AdvancedImageBox, Rect>(nameof(SelectionRegion), Rect.Empty, notifying: (control, before) => SelectionRegionChanged((AdvancedImageBox)control, before));


        public Rect SelectionRegion
        {
            get => GetValue(SelectionRegionProperty);
            set
            {
                SetValue(SelectionRegionProperty, value);
            }
        }

        public Rectangle SelectionRegionNet
        {
            get
            {
                var rect = SelectionRegion;
                return new Rectangle((int)Math.Ceiling(rect.X), (int)Math.Ceiling(rect.Y),
                    (int)rect.Width, (int)rect.Height);
            }
        }

        public PixelSize SelectionPixelSize
        {
            get
            {
                var rect = SelectionRegion;
                return new PixelSize((int)rect.Width, (int)rect.Height);
            }
        }

        public bool HaveSelection => !SelectionRegion.IsEmpty;
        #endregion

        #region Constructor
        public AdvancedImageBox()
        {
            InitializeComponent();

            //FocusableProperty.OverrideDefaultValue(typeof(AdvancedImageBox), true);
            AffectsRender<AdvancedImageBox>(ShowGridProperty);

            HorizontalScrollBar = this.FindControl<ScrollBar>("HorizontalScrollBar");
            VerticalScrollBar = this.FindControl<ScrollBar>("VerticalScrollBar");
            ViewPort = this.FindControl<ContentPresenter>("ViewPort");
            //Mip = this.FindControl<Image>("Mip");

            SizeModeChanged();

            HorizontalScrollBar.Scroll += ScrollBarOnScroll;
            VerticalScrollBar.Scroll += ScrollBarOnScroll;
            ViewPort.PointerPressed += ViewPortOnPointerPressed;
            ViewPort.PointerLeave += ViewPortOnPointerLeave;
            ViewPort.PointerMoved += ViewPortOnPointerMoved;
            ViewPort.PointerWheelChanged += ViewPortOnPointerWheelChanged;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        #endregion

        public bool IsSelectionMode => (SelectWithMouseButtons & MouseButtons.LeftButton) != 0;
        public bool IsPaintMode => (PaintWithMouseButtons & MouseButtons.LeftButton) != 0 || (EyeDropWithMouseButtons & MouseButtons.LeftButton) != 0;

        public bool IsMaskMode => (MaskWithMouseButtons & MouseButtons.LeftButton) != 0;

        private void UpdateCursor()
        {
            if (_pointerPosition is { X: >= 0, Y: >= 0 })
            {
                if (IsSelectionMode)
                {
                    var imagePoint = PointToImage(_pointerPosition);
                    if (CurrentDraggingMode == DraggingMode.TopLeft || IsNear(imagePoint, SelectionRegion.TopLeft)
                        || CurrentDraggingMode == DraggingMode.BottomRight || IsNear(imagePoint, SelectionRegion.BottomRight))
                    {
                        Cursor = Cursors[(int)CursorsTypes.ResizeTopLeft];
                    }
                    else if (CurrentDraggingMode == DraggingMode.TopRight || IsNear(imagePoint, SelectionRegion.TopRight)
                        || CurrentDraggingMode == DraggingMode.BottomLeft || IsNear(imagePoint, SelectionRegion.BottomLeft))
                    {
                        Cursor = Cursors[(int)CursorsTypes.ResizeTopRight];
                    }
                    else if (CurrentDraggingMode == DraggingMode.Full || SelectionRegion.Contains(imagePoint))
                    {
                        Cursor = Cursors[(int)CursorsTypes.DragSelection];
                        return;
                    }
                    else
                    {
                        Cursor = Cursors[(int)CursorsTypes.SelectionMode];
                    }
                }

                if (IsPaintMode || IsMaskMode)
                {
                    Cursor = Cursors[(int)CursorsTypes.PaintMode];
                    return;
                }

                if (_isPanning)
                {
                    Cursor = Cursors[(int)CursorsTypes.Panning];
                    return;
                }
            }
        }
        #region Render methods
        public void TriggerRender(bool renderOnlyCursorTracker = false)
        {
            if (!_canRender) return;
            //if (renderOnlyCursorTracker && _trackerImage is null) return;
            InvalidateVisual();
        }


        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var viewPortSize = ViewPortSize;
            // Draw Grid
            var gridCellSize = GridCellSize;
            if (ShowGrid & gridCellSize > 0 && (!IsHorizontalBarVisible || !IsVerticalBarVisible))
            {
                var gridColor = GridColor;
                var altColor = GridColorAlternate;
                var currentColor = gridColor;
                for (int y = 0; y < viewPortSize.Height; y += gridCellSize)
                {
                    var firstRowColor = currentColor;

                    for (int x = 0; x < viewPortSize.Width; x += gridCellSize)
                    {
                        context.FillRectangle(currentColor, new Rect(x, y, gridCellSize, gridCellSize));
                        currentColor = ReferenceEquals(currentColor, gridColor) ? altColor : gridColor;
                    }

                    if (Equals(firstRowColor, currentColor))
                        currentColor = ReferenceEquals(currentColor, gridColor) ? altColor : gridColor;
                }
            }

            var toDraw = Image;
            if (toDraw is null) return;
            var imageViewPort = GetImageViewPort();

            //var targetImage = bitmapMips.ContainsKey(image) ? bitmapMips[image].First() : image;
            IEnumerable<Bitmap> paintLayers = this.paintLayers[toDraw];
            if (mipScaleFactor > 1)
            {
                var mipSet = mips[(toDraw, mipScaleFactor)];
                toDraw = mipSet.image;
                paintLayers = mipSet.paintLayers;
            }

            // Draw image
            context.DrawImage(toDraw,
                GetSourceImageRegion(mipScaleFactor),
                imageViewPort
            );

            if (paintLayers != null)
            {
                foreach (var bitmap in paintLayers)
                {
                    context.DrawImage(bitmap,
                    GetSourceImageRegion(mipScaleFactor),
                    imageViewPort);
                }
            }


            if (maskLayers != null && (MaskWithMouseButtons != MouseButtons.None))
            {
                IEnumerable<Bitmap> maskLayers = this.maskLayers[Image];
                if (mipScaleFactor > 1)
                {
                    var mipSet = mips[(Image, mipScaleFactor)];
                    maskLayers = mipSet.maskLayers;
                }

                foreach (var bitmap in maskLayers)
                {
                    context.DrawImage(bitmap,
                    GetSourceImageRegion(mipScaleFactor),
                    imageViewPort);
                }
            }

            var zoomFactor = ZoomFactor;

            if ((PaintWithMouseButtons & MouseButtons.LeftButton) != 0 && _pointerPosition is { X: >= 0, Y: >= 0 })
            {
                context.DrawEllipse(new SolidColorBrush(PaintBrushColor), null, _pointerPosition, (PaintBrushSize - 1) * zoomFactor, (PaintBrushSize - 1) * zoomFactor);
            }

            if (HaveTrackerImage && _pointerPosition is { X: >= 0, Y: >= 0 })
            {
                var destSize = TrackerImageAutoZoom
                    ? new Size(_trackerImage!.Size.Width * zoomFactor, _trackerImage.Size.Height * zoomFactor)
                    : toDraw.Size;

                var destPos = new Point(
                    _pointerPosition.X - destSize.Width / 2,
                    _pointerPosition.Y - destSize.Height / 2
                );
                context.DrawImage(_trackerImage,
                    new Rect(destPos, destSize)
                );
            }

            //SkiaContext.SkCanvas.dr
            // Draw pixel grid
            if (zoomFactor > PixelGridZoomThreshold && SizeMode == SizeModes.Normal)
            {
                var offsetX = Offset.X % zoomFactor;
                var offsetY = Offset.Y % zoomFactor;

                Pen pen = new(PixelGridColor);
                for (double x = imageViewPort.X + zoomFactor - offsetX; x < imageViewPort.Right; x += zoomFactor)
                {
                    context.DrawLine(pen, new Point(x, imageViewPort.X), new Point(x, imageViewPort.Bottom));
                }

                for (double y = imageViewPort.Y + zoomFactor - offsetY; y < imageViewPort.Bottom; y += zoomFactor)
                {
                    context.DrawLine(pen, new Point(imageViewPort.Y, y), new Point(imageViewPort.Right, y));
                }

                context.DrawRectangle(pen, imageViewPort);
            }

            if (!SelectionRegion.IsEmpty)
            {
                var rect = GetOffsetRectangle(SelectionRegion);
                var selectionColor = SelectionColor;
                context.FillRectangle(selectionColor, rect);
                var color = Color.FromArgb(255, selectionColor.Color.R, selectionColor.Color.G, selectionColor.Color.B);
                context.DrawRectangle(new Pen(color.ToUint32()), rect);
            }
        }

        private bool UpdateViewPort()
        {
            if (Image is null)
            {
                HorizontalScrollBar.Maximum = 0;
                VerticalScrollBar.Maximum = 0;
                return true;
            }

            var scaledImageWidth = ScaledImageWidth;
            var scaledImageHeight = ScaledImageHeight;
            var width = scaledImageWidth - HorizontalScrollBar.ViewportSize;
            var height = scaledImageHeight - VerticalScrollBar.ViewportSize;
            //var width = scaledImageWidth <= Viewport.Width ? Viewport.Width : scaledImageWidth;
            //var height = scaledImageHeight <= Viewport.Height ? Viewport.Height : scaledImageHeight;

            bool changed = false;
            if (Math.Abs(HorizontalScrollBar.Maximum - width) > 0.01)
            {
                HorizontalScrollBar.Maximum = width;
                changed = true;
            }

            if (Math.Abs(VerticalScrollBar.Maximum - scaledImageHeight) > 0.01)
            {
                VerticalScrollBar.Maximum = height;
                changed = true;
            }

            /*if (changed)
            {
                var newContainer = new ContentControl
                {
                    Width = width,
                    Height = height
                };
                FillContainer.Content = SizedContainer = newContainer;
                Debug.WriteLine($"Updated ViewPort: {DateTime.Now.Ticks}");
                //TriggerRender();
            }*/

            UpdateRenderedMipLevel();

            return changed;
        }
        #endregion

        #region Events and Overrides

        private void ScrollBarOnScroll(object? sender, ScrollEventArgs e)
        {
            TriggerRender();
        }

        /*protected override void OnScrollChanged(ScrollChangedEventArgs e)
        {
            Debug.WriteLine($"ViewportDelta: {e.ViewportDelta} | OffsetDelta: {e.OffsetDelta} | ExtentDelta: {e.ExtentDelta}");
            if (!e.ViewportDelta.IsDefault)
            {
                UpdateViewPort();
            }

            TriggerRender();

            base.OnScrollChanged(e);
        }*/

        private void ViewPortOnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            e.Handled = true;
            if (Image is null) return;
            if (AllowZoom && SizeMode == SizeModes.Normal)
            {
                // The MouseWheel event can contain multiple "spins" of the wheel so we need to adjust accordingly
                //double spins = Math.Abs(e.Delta.Y);
                //Debug.WriteLine(e.GetPosition(this));
                // TODO: Really should update the source method to handle multiple increments rather than calling it multiple times
                /*for (int i = 0; i < spins; i++)
                {*/
                ProcessMouseZoom(e.Delta.Y > 0, e.GetPosition(ViewPort));
                //}
            }
        }

        bool IsNear(Point p1, Point p2)
        {
            return Math.Abs(p1.X - p2.X) < 10.0 && Math.Abs(p1.Y - p2.Y) < 10.0;
        }
        private void ViewPortOnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.Handled
                || _isPainting
                || _isMasking
                || _isPanning
                || _isSelecting
                || IsDraggingSelection
                || Image is null) return;

            var pointer = e.GetCurrentPoint(this);


            if (
                    pointer.Properties.IsLeftButtonPressed && (SelectWithMouseButtons & MouseButtons.LeftButton) != 0 ||
                    pointer.Properties.IsMiddleButtonPressed && (SelectWithMouseButtons & MouseButtons.MiddleButton) != 0 ||
                    pointer.Properties.IsRightButtonPressed && (SelectWithMouseButtons & MouseButtons.RightButton) != 0
               )
            {

                var convertedPosition = PointToImage(pointer.Position);
                if (IsNear(SelectionRegion.TopLeft, convertedPosition))
                {
                    CurrentDraggingMode = DraggingMode.TopLeft;
                }
                else if (IsNear(SelectionRegion.TopRight, convertedPosition))
                {
                    CurrentDraggingMode = DraggingMode.TopRight;
                }
                else if (IsNear(SelectionRegion.BottomLeft, convertedPosition))
                {
                    CurrentDraggingMode = DraggingMode.BottomLeft;
                }
                else if (IsNear(SelectionRegion.BottomRight, convertedPosition))
                {
                    CurrentDraggingMode = DraggingMode.BottomRight;
                }
                else if (SelectionRegion.Contains(convertedPosition))
                {
                    CurrentDraggingMode = DraggingMode.Full;
                    _dragRelativePosition = convertedPosition - SelectionRegion.TopLeft;
                }
                else
                {
                    IsSelecting = true;
                }
            }
            else if (
                    pointer.Properties.IsLeftButtonPressed && (PaintWithMouseButtons & MouseButtons.LeftButton) != 0 ||
                    pointer.Properties.IsMiddleButtonPressed && (PaintWithMouseButtons & MouseButtons.MiddleButton) != 0 ||
                    pointer.Properties.IsRightButtonPressed && (PaintWithMouseButtons & MouseButtons.RightButton) != 0
               )
            {
                IsPainting = true;
                paintLayers[Image].Add(new RenderTargetBitmap(Image.PixelSize));
                IsPainted = true;
            }
            else if (
                    pointer.Properties.IsLeftButtonPressed && (MaskWithMouseButtons & MouseButtons.LeftButton) != 0 ||
                    pointer.Properties.IsMiddleButtonPressed && (MaskWithMouseButtons & MouseButtons.MiddleButton) != 0 ||
                    pointer.Properties.IsRightButtonPressed && (MaskWithMouseButtons & MouseButtons.RightButton) != 0
               )
            {
                IsMasking = true;
                maskLayers[Image].Add(new RenderTargetBitmap(Image.PixelSize));
            }
            else if (
                    pointer.Properties.IsLeftButtonPressed && (EyeDropWithMouseButtons & MouseButtons.LeftButton) != 0 ||
                    pointer.Properties.IsMiddleButtonPressed && (EyeDropWithMouseButtons & MouseButtons.MiddleButton) != 0 ||
                    pointer.Properties.IsRightButtonPressed && (EyeDropWithMouseButtons & MouseButtons.RightButton) != 0
               )
            {
                //This is some hacky garbage, create a writable bitmap (which we can read pixel data from ) image, ciopy the entire image into it, then read the 1 pixel
                var convertedPosition = PointToImage(pointer.Position);
                if (convertedPosition.X > 0 && convertedPosition.Y > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        Image.Save(memoryStream);
                        memoryStream.Position = 0;
                        using var writable = WriteableBitmap.Decode(memoryStream);
                        using var buffer = writable.Lock();
                        PaintBrushColor = buffer.GetPixelColor((int)convertedPosition.X, (int)convertedPosition.Y);
                    }
                }
            }
            else if ((pointer.Properties.IsLeftButtonPressed && (PanWithMouseButtons & MouseButtons.LeftButton) != 0
                 || pointer.Properties.IsMiddleButtonPressed && (PanWithMouseButtons & MouseButtons.MiddleButton) != 0
                 || pointer.Properties.IsRightButtonPressed && (PanWithMouseButtons & MouseButtons.RightButton) != 0)
                 && AutoPan
                 && SizeMode == SizeModes.Normal)
            {
                IsPanning = true;
            }

            var location = pointer.Position;

            if (location.X > ViewPortSize.Width) return;
            if (location.Y > ViewPortSize.Height) return;
            _startMousePosition = location;
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (e.Handled) return;

            IsMasking = false;
            IsPanning = false;
            IsSelecting = false;
            CurrentDraggingMode = DraggingMode.None;
            IsPainting = false;
        }

        private void ViewPortOnPointerLeave(object? sender, PointerEventArgs e)
        {
            PointerPosition = new Point(-1, -1);
            TriggerRender(true);
            e.Handled = true;
        }

        private void ViewPortOnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (e.Handled) return;

            UpdateCursor();

            var pointer = e.GetCurrentPoint(ViewPort);
            PointerPosition = pointer.Position;

            if (Image == null || (!_isPanning && !_isSelecting && !IsDraggingSelection && !_isPainting && !_isMasking))
            {
                TriggerRender(true);
                return;
            }

            if (_isPainting)
            {
                var newestLayer = paintLayers[Image].Last();
                using (var bitmapRenderContext = newestLayer.CreateDrawingContext(null))
                {
                    var brushSize = PaintBrushSize - 1;
                    var pos = PointToImage(pointer.Position);
                    var brush = new SolidColorBrush(PaintBrushColor);
                    var pen = new Pen(brush);
                    var d = brushSize * 2.0;
                    if (d == 0)
                    {
                        d = 0.5;
                    }
                    bitmapRenderContext.DrawEllipse(brush, pen, new Rect(pos.X - brushSize, pos.Y - brushSize, d, d));

                    var intermediatePoints = e.GetIntermediatePoints(this);
                    foreach (var point in intermediatePoints)
                    {
                        pos = PointToImage(point.Position);
                        bitmapRenderContext.DrawEllipse(brush, pen, new Rect(pos.X - brushSize, pos.Y - brushSize, d, d));
                    }
                    RenderTargetBitmap layerMip = null;
                    //Draw to the current active layer mip
                    if (mipScaleFactor > 1)
                    {
                        var layerMips = mips[(Image, mipScaleFactor)].paintLayers;

                        if (layerMips.Count() < paintLayers[Image].Count())
                        {
                            layerMip = new RenderTargetBitmap(new PixelSize(Image.PixelSize.Width / mipScaleFactor, Image.PixelSize.Height / mipScaleFactor));
                            layerMips.Add(layerMip);
                        }
                        else
                            layerMip = layerMips.Last();

                        using (var mipRenderContext = layerMip.CreateDrawingContext(null))
                        {
                            mipRenderContext.DrawEllipse(brush, pen, new Rect(pos.X / mipScaleFactor - brushSize / mipScaleFactor, pos.Y / mipScaleFactor - brushSize / mipScaleFactor, d / mipScaleFactor, d / mipScaleFactor));
                            foreach (var point in intermediatePoints)
                            {
                                pos = PointToImage(point.Position);
                                mipRenderContext.DrawEllipse(brush, pen, new Rect(pos.X / mipScaleFactor - brushSize / mipScaleFactor, pos.Y / mipScaleFactor - brushSize / mipScaleFactor, d / mipScaleFactor, d / mipScaleFactor));
                            }
                        }
                    }
                }
                TriggerRender(false);
            }
            if (_isMasking)
            {
                var newestLayer = maskLayers[Image].Last();
                using (var bitmapRenderContext = newestLayer.CreateDrawingContext(null))
                {
                    var brushSize = MaskBrushSize - 1;
                    var pos = PointToImage(pointer.Position);
                    var brush = new SolidColorBrush(new Color(255, 0, 0, 0));
                    var pen = new Pen(brush);
                    var d = brushSize * 2.0;
                    if (d == 0)
                    {
                        d = 0.5;
                    }
                    bitmapRenderContext.DrawEllipse(brush, pen, new Rect(pos.X - brushSize, pos.Y - brushSize, d, d));

                    var intermediatePoints = e.GetIntermediatePoints(this);
                    foreach (var point in intermediatePoints)
                    {
                        pos = PointToImage(point.Position);
                        bitmapRenderContext.DrawEllipse(brush, pen, new Rect(pos.X - brushSize, pos.Y - brushSize, d, d));
                    }
                    RenderTargetBitmap layerMip = null;
                    //Draw to the current active layer mip
                    if (mipScaleFactor > 1)
                    {
                        var layerMips = mips[(Image, mipScaleFactor)].maskLayers;

                        if (layerMips.Count() < maskLayers[Image].Count())
                        {
                            layerMip = new RenderTargetBitmap(new PixelSize(Image.PixelSize.Width / mipScaleFactor, Image.PixelSize.Height / mipScaleFactor));
                            layerMips.Add(layerMip);
                        }
                        else
                            layerMip = layerMips.Last();

                        using (var mipRenderContext = layerMip.CreateDrawingContext(null))
                        {
                            mipRenderContext.DrawEllipse(brush, pen, new Rect(pos.X / mipScaleFactor - brushSize / mipScaleFactor, pos.Y / mipScaleFactor - brushSize / mipScaleFactor, d / mipScaleFactor, d / mipScaleFactor));
                            foreach (var point in intermediatePoints)
                            {
                                pos = PointToImage(point.Position);
                                mipRenderContext.DrawEllipse(brush, pen, new Rect(pos.X / mipScaleFactor - brushSize / mipScaleFactor, pos.Y / mipScaleFactor - brushSize / mipScaleFactor, d / mipScaleFactor, d / mipScaleFactor));
                            }
                        }
                    }
                }
                TriggerRender(false);
            }
            if (_isPanning)
            {
                double x;
                double y;

                if (!InvertMousePan)
                {
                    x = _startScrollPosition.X + (_startMousePosition.X - _pointerPosition.X);
                    y = _startScrollPosition.Y + (_startMousePosition.Y - _pointerPosition.Y);
                }
                else
                {
                    x = (_startScrollPosition.X - (_startMousePosition.X - _pointerPosition.X));
                    y = (_startScrollPosition.Y - (_startMousePosition.Y - _pointerPosition.Y));
                }

                Offset = new Vector(x, y);
            }
            else if (IsDraggingSelection || _isSelecting)
            {
                if (Image != null)
                {
                    var viewPortPoint = new Point(
                        Math.Min(_pointerPosition.X, ViewPort.Bounds.Right),
                        Math.Min(_pointerPosition.Y, ViewPort.Bounds.Bottom));

                    var imagePoint = PointToImage(viewPortPoint, true);
                    var imageSize = Image.Size;

                    Point newTopLeft = SelectionRegion.TopLeft;
                    Point newBottomRight = SelectionRegion.BottomRight;
                    var draggingMode = CurrentDraggingMode;
                    if (_isSelecting)
                    {
                        var originImagePoint = PointToImage(_startMousePosition, true);
                        if (_pointerPosition.X < _startMousePosition.X && _pointerPosition.Y < _startMousePosition.Y)
                        {
                            newTopLeft = imagePoint;
                            newBottomRight = originImagePoint;
                            draggingMode = DraggingMode.TopLeft;
                        }

                        if (_pointerPosition.X < _startMousePosition.X && _pointerPosition.Y >= _startMousePosition.Y)
                        {
                            newTopLeft = new Point(imagePoint.X, originImagePoint.Y);
                            newBottomRight = new Point(originImagePoint.X, imagePoint.Y);
                            draggingMode = DraggingMode.BottomLeft;
                        }

                        if (_pointerPosition.X >= _startMousePosition.X && _pointerPosition.Y < _startMousePosition.Y)
                        {
                            newTopLeft = new Point(originImagePoint.X, imagePoint.Y);
                            newBottomRight = new Point(imagePoint.X, originImagePoint.Y);
                            draggingMode = DraggingMode.TopRight;
                        }

                        if (_pointerPosition.X >= _startMousePosition.X && _pointerPosition.Y >= _startMousePosition.Y)
                        {
                            newTopLeft = originImagePoint;
                            newBottomRight = imagePoint;
                            draggingMode = DraggingMode.BottomRight;
                        }
                    }

                    if (draggingMode == DraggingMode.TopLeft)
                    {
                        newTopLeft = new Point(Math.Floor(imagePoint.X), Math.Ceiling(imagePoint.Y));
                        if (IsAspectRatioLocked)
                        {
                            var curWidth = Math.Abs(newTopLeft.X - newBottomRight.X);
                            var curheight = Math.Abs(newTopLeft.Y - newBottomRight.Y);
                            var newAspectRatio = curWidth / curheight;
                            if (newAspectRatio > _lockedAspectRatio)
                            {
                                newTopLeft = new Point(newTopLeft.X, newBottomRight.Y - Math.Floor(curWidth / _lockedAspectRatio));
                                if (newTopLeft.Y < 0)
                                {
                                    newTopLeft = new Point(newBottomRight.X - Math.Floor(newBottomRight.Y * _lockedAspectRatio), 0);
                                }
                            }
                            else
                            {
                                newTopLeft = new Point(newBottomRight.X - Math.Floor(curheight * _lockedAspectRatio), newTopLeft.Y);
                                if (newTopLeft.X < 0)
                                {
                                    newTopLeft = new Point(0, newBottomRight.Y - Math.Floor(newBottomRight.X / _lockedAspectRatio));
                                }
                            }
                        }
                    }
                    else if (draggingMode == DraggingMode.TopRight)
                    {
                        newTopLeft = new Point(newTopLeft.X, Math.Ceiling(imagePoint.Y));
                        newBottomRight = new Point(Math.Floor(imagePoint.X), newBottomRight.Y);
                        if (IsAspectRatioLocked)
                        {
                            var curWidth = Math.Abs(newTopLeft.X - newBottomRight.X);
                            var curheight = Math.Abs(newTopLeft.Y - newBottomRight.Y);
                            var newAspectRatio = curWidth / curheight;
                            if (newAspectRatio < _lockedAspectRatio)
                            {
                                newBottomRight = new Point(newTopLeft.X + Math.Floor(curheight * _lockedAspectRatio), newBottomRight.Y);
                                if (newBottomRight.X >= Image.PixelSize.Width)
                                {
                                    newBottomRight = new Point(Image.PixelSize.Width - 1, newBottomRight.Y);
                                    newTopLeft = new Point(newTopLeft.X, newBottomRight.Y - Math.Floor((newBottomRight.X - newTopLeft.X) / _lockedAspectRatio));
                                }
                            }
                            else
                            {
                                newTopLeft = new Point(newTopLeft.X, newBottomRight.Y - Math.Floor(curWidth / _lockedAspectRatio));
                                if (newTopLeft.Y < 0)
                                {
                                    newTopLeft = new Point(newTopLeft.X, 0);
                                    newBottomRight = new Point(newTopLeft.X + Math.Floor((newBottomRight.Y - newTopLeft.Y) * _lockedAspectRatio), newBottomRight.Y);
                                }
                            }
                        }
                    }
                    else if (draggingMode == DraggingMode.BottomLeft)
                    {
                        newTopLeft = new Point(imagePoint.X, newTopLeft.Y);
                        newBottomRight = new Point(newBottomRight.X, imagePoint.Y);
                        if (IsAspectRatioLocked)
                        {
                            var curWidth = Math.Abs(newTopLeft.X - newBottomRight.X);
                            var curheight = Math.Abs(newTopLeft.Y - newBottomRight.Y);
                            var newAspectRatio = curWidth / curheight;
                            if (newAspectRatio < _lockedAspectRatio)
                            {
                                newTopLeft = new Point(newBottomRight.X - Math.Floor(curheight * _lockedAspectRatio), newTopLeft.Y);
                                if (newTopLeft.X < 0)
                                {
                                    newTopLeft = new Point(0, newTopLeft.Y);
                                    newBottomRight = new Point(newBottomRight.X, newTopLeft.Y + Math.Floor(newBottomRight.X / _lockedAspectRatio));
                                }
                            }
                            else
                            {
                                newBottomRight = new Point(newBottomRight.X, newTopLeft.Y + Math.Floor(curWidth / _lockedAspectRatio));
                                if (newBottomRight.Y >= Image.PixelSize.Height)
                                {
                                    newBottomRight = new Point(newBottomRight.X, Image.PixelSize.Height - 1);
                                    newTopLeft = new Point(newBottomRight.X - Math.Floor((newBottomRight.Y - newTopLeft.Y) * _lockedAspectRatio), newTopLeft.Y);
                                }
                            }
                        }
                    }
                    else if (draggingMode == DraggingMode.BottomRight)
                    {
                        newBottomRight = imagePoint;
                        if (IsAspectRatioLocked)
                        {
                            var curWidth = Math.Abs(newTopLeft.X - newBottomRight.X);
                            var curheight = Math.Abs(newTopLeft.Y - newBottomRight.Y);
                            var newAspectRatio = curWidth / curheight;
                            if (newAspectRatio < _lockedAspectRatio)
                            {
                                newBottomRight = new Point(newTopLeft.X + Math.Floor(curheight * _lockedAspectRatio), newBottomRight.Y);
                                if (newBottomRight.X >= Image.PixelSize.Width)
                                {
                                    var xpos = Image.PixelSize.Width - 1;
                                    newBottomRight = new Point(xpos, newTopLeft.Y + Math.Floor((xpos - newTopLeft.X) / _lockedAspectRatio));
                                }
                            }
                            else
                            {
                                newBottomRight = new Point(newBottomRight.X, newTopLeft.Y + Math.Floor(curWidth / _lockedAspectRatio));
                                if (newBottomRight.Y >= Image.PixelSize.Height)
                                {
                                    var ypos = Image.PixelSize.Height - 1;
                                    newBottomRight = new Point(newTopLeft.X + Math.Floor((ypos - newTopLeft.Y) * _lockedAspectRatio), ypos);
                                }
                            }
                        }
                    }
                    else
                    {
                        newTopLeft = new Point(Math.Max(Math.Floor(imagePoint.X - _dragRelativePosition.X), 0),
                                                   Math.Max(Math.Floor(imagePoint.Y - _dragRelativePosition.Y), 0));
                        newBottomRight = new Point(Math.Min(Math.Ceiling(newTopLeft.X + SelectionRegion.Width), Image.Size.Width),
                                                       Math.Min(Math.Ceiling(newTopLeft.Y + SelectionRegion.Height), Image.Size.Height));
                        newTopLeft = new Point(newBottomRight.X - SelectionRegion.Width, newBottomRight.Y - SelectionRegion.Height);
                    }
                    SelectionRegion = new Rect(newTopLeft, newBottomRight);
                }
            }
            else if (_isSelecting)
            {
                var viewPortPoint = new Point(
                    Math.Min(_pointerPosition.X, ViewPort.Bounds.Right),
                    Math.Min(_pointerPosition.Y, ViewPort.Bounds.Bottom));

                double x;
                double y;
                double endx;
                double endy;

                var imageOffset = GetImageViewPort().Position;

                if (viewPortPoint.X < _startMousePosition.X)
                {
                    x = viewPortPoint.X;
                    endx = _startMousePosition.X;
                }
                else
                {
                    x = _startMousePosition.X;
                    endx = viewPortPoint.X;
                }

                if (viewPortPoint.Y < _startMousePosition.Y)
                {
                    y = viewPortPoint.Y;
                    endy = _startMousePosition.Y;
                }
                else
                {
                    y = _startMousePosition.Y;
                    endy = viewPortPoint.Y;
                }

                x -= imageOffset.X - Offset.X;
                endx -= imageOffset.X - Offset.X;

                y -= imageOffset.Y - Offset.Y;
                endy -= imageOffset.Y - Offset.Y;

                var zoomFactor = ZoomFactor;
                x /= zoomFactor;
                y /= zoomFactor;
                endx /= zoomFactor;
                endy /= zoomFactor;

                x = Math.Floor(x);
                y = Math.Floor(y);
                endx = Math.Ceiling(endx);
                endy = Math.Ceiling(endy);

                var w = endx - x;
                var h = endy - y;

                if (w > 0 && h > 0)
                {
                    SelectionRegion = FitRectangle(new Rect(x, y, w, h));


                    if (IsAspectRatioLocked)
                    {
                        var newAspectRatio = SelectionRegion.Width / SelectionRegion.Height;
                        if (newAspectRatio > _lockedAspectRatio)
                        {

                        }
                    }
                }
            }

            e.Handled = true;
        }

        #endregion

        #region Zoom and Size modes
        private void ProcessMouseZoom(bool isZoomIn, Point cursorPosition)
            => PerformZoom(isZoomIn ? ZoomActions.ZoomIn : ZoomActions.ZoomOut, true, cursorPosition);

        /// <summary>
        /// Returns an appropriate zoom level based on the specified action, relative to the current zoom level.
        /// </summary>
        /// <param name="action">The action to determine the zoom level.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if an unsupported action is specified.</exception>
        private int GetZoomLevel(ZoomActions action)
        {
            var result = action switch
            {
                ZoomActions.None => Zoom,
                ZoomActions.ZoomIn => _zoomLevels.NextZoom(Zoom),
                ZoomActions.ZoomOut => _zoomLevels.PreviousZoom(Zoom),
                ZoomActions.ActualSize => 100,
                _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
            };
            return result;
        }

        /// <summary>
        /// Resets the <see cref="SizeModes"/> property whilsts retaining the original <see cref="Zoom"/>.
        /// </summary>
        protected void RestoreSizeMode()
        {
            if (SizeMode != SizeModes.Normal)
            {
                var previousZoom = Zoom;
                SizeMode = SizeModes.Normal;
                Zoom = previousZoom; // Stop the zoom getting reset to 100% before calculating the new zoom
            }
        }

        private void PerformZoom(ZoomActions action, bool preservePosition)
            => PerformZoom(action, preservePosition, CenterPoint);

        private void PerformZoom(ZoomActions action, bool preservePosition, Point relativePoint)
        {
            Point currentPixel = PointToImage(relativePoint);
            int currentZoom = Zoom;
            int newZoom = GetZoomLevel(action);

            /*if (preservePosition && Zoom != currentZoom)
                CanRender = false;*/

            RestoreSizeMode();
            Zoom = newZoom;

            if (preservePosition && Zoom != currentZoom)
            {
                ScrollTo(currentPixel, relativePoint);
            }
        }

        /// <summary>
        ///   Zooms into the image
        /// </summary>
        public void ZoomIn()
            => ZoomIn(true);

        /// <summary>
        ///   Zooms into the image
        /// </summary>
        /// <param name="preservePosition"><c>true</c> if the current scrolling position should be preserved relative to the new zoom level, <c>false</c> to reset.</param>
        public void ZoomIn(bool preservePosition)
        {
            PerformZoom(ZoomActions.ZoomIn, preservePosition);
        }

        /// <summary>
        ///   Zooms out of the image
        /// </summary>
        public void ZoomOut()
            => ZoomOut(true);

        /// <summary>
        ///   Zooms out of the image
        /// </summary>
        /// <param name="preservePosition"><c>true</c> if the current scrolling position should be preserved relative to the new zoom level, <c>false</c> to reset.</param>
        public void ZoomOut(bool preservePosition)
        {
            PerformZoom(ZoomActions.ZoomOut, preservePosition);
        }

        /// <summary>
        /// Zooms to the maximum size for displaying the entire image within the bounds of the control.
        /// </summary>
        public void ZoomToFit()
        {
            if (!IsImageLoaded) return;
            Zoom = ZoomLevelToFit;
        }

        /// <summary>
        ///   Adjusts the view port to fit the given region
        /// </summary>
        /// <param name="x">The X co-ordinate of the selection region.</param>
        /// <param name="y">The Y co-ordinate of the selection region.</param>
        /// <param name="width">The width of the selection region.</param>
        /// <param name="height">The height of the selection region.</param>
        /// <param name="margin">Give a margin to rectangle by a value to zoom-out that pixel value</param>
        public void ZoomToRegion(double x, double y, double width, double height, double margin = 0)
        {
            ZoomToRegion(new Rect(x, y, width, height), margin);
        }

        /// <summary>
        ///   Adjusts the view port to fit the given region
        /// </summary>
        /// <param name="x">The X co-ordinate of the selection region.</param>
        /// <param name="y">The Y co-ordinate of the selection region.</param>
        /// <param name="width">The width of the selection region.</param>
        /// <param name="height">The height of the selection region.</param>
        /// <param name="margin">Give a margin to rectangle by a value to zoom-out that pixel value</param>
        public void ZoomToRegion(int x, int y, int width, int height, double margin = 0)
        {
            ZoomToRegion(new Rect(x, y, width, height), margin);
        }

        /// <summary>
        ///   Adjusts the view port to fit the given region
        /// </summary>
        /// <param name="rectangle">The rectangle to fit the view port to.</param>
        /// <param name="margin">Give a margin to rectangle by a value to zoom-out that pixel value</param>
        public void ZoomToRegion(Rectangle rectangle, double margin = 0) =>
            ZoomToRegion(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, margin);

        /// <summary>
        ///   Adjusts the view port to fit the given region
        /// </summary>
        /// <param name="rectangle">The rectangle to fit the view port to.</param>
        /// <param name="margin">Give a margin to rectangle by a value to zoom-out that pixel value</param>
        public void ZoomToRegion(Rect rectangle, double margin = 0)
        {
            if (margin > 0) rectangle = rectangle.Inflate(margin);
            var ratioX = ViewPortSize.Width / rectangle.Width;
            var ratioY = ViewPortSize.Height / rectangle.Height;
            var zoomFactor = Math.Min(ratioX, ratioY);
            var cx = rectangle.X + rectangle.Width / 2;
            var cy = rectangle.Y + rectangle.Height / 2;

            CanRender = false;
            Zoom = (int)(zoomFactor * 100); // This function sets the zoom so viewport will change
            CenterAt(new Point(cx, cy)); // If i call this here, it will move to the wrong position due wrong viewport
        }

        /// <summary>
        /// Zooms to current selection region
        /// </summary>
        public void ZoomToSelectionRegion(double margin = 0)
        {
            if (!HaveSelection) return;
            ZoomToRegion(SelectionRegion, margin);
        }

        /// <summary>
        /// Resets the zoom to 100%.
        /// </summary>
        public void PerformActualSize()
        {
            SizeMode = SizeModes.Normal;
            //SetZoom(100, ImageZoomActions.ActualSize | (Zoom < 100 ? ImageZoomActions.ZoomIn : ImageZoomActions.ZoomOut));
            Zoom = 100;
        }
        #endregion

        #region Utility methods
        /// <summary>
        ///   Determines whether the specified point is located within the image view port
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>
        ///   <c>true</c> if the specified point is located within the image view port; otherwise, <c>false</c>.
        /// </returns>
        public bool IsPointInImage(Point point)
            => GetImageViewPort().Contains(point);

        /// <summary>
        ///   Determines whether the specified point is located within the image view port
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to check.</param>
        /// <param name="y">The Y co-ordinate of the point to check.</param>
        /// <returns>
        ///   <c>true</c> if the specified point is located within the image view port; otherwise, <c>false</c>.
        /// </returns>
        public bool IsPointInImage(int x, int y)
            => IsPointInImage(new Point(x, y));

        /// <summary>
        ///   Determines whether the specified point is located within the image view port
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to check.</param>
        /// <param name="y">The Y co-ordinate of the point to check.</param>
        /// <returns>
        ///   <c>true</c> if the specified point is located within the image view port; otherwise, <c>false</c>.
        /// </returns>
        public bool IsPointInImage(double x, double y)
            => IsPointInImage(new Point(x, y));

        /// <summary>
        ///   Converts the given client size point to represent a coordinate on the source image.
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to convert.</param>
        /// <param name="y">The Y co-ordinate of the point to convert.</param>
        /// <param name="fitToBounds">
        ///   if set to <c>true</c> and the point is outside the bounds of the source image, it will be mapped to the nearest edge.
        /// </param>
        /// <returns><c>Point.Empty</c> if the point could not be matched to the source image, otherwise the new translated point</returns>
        public Point PointToImage(double x, double y, bool fitToBounds = true)
            => PointToImage(new Point(x, y), fitToBounds);

        /// <summary>
        ///   Converts the given client size point to represent a coordinate on the source image.
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to convert.</param>
        /// <param name="y">The Y co-ordinate of the point to convert.</param>
        /// <param name="fitToBounds">
        ///   if set to <c>true</c> and the point is outside the bounds of the source image, it will be mapped to the nearest edge.
        /// </param>
        /// <returns><c>Point.Empty</c> if the point could not be matched to the source image, otherwise the new translated point</returns>
        public Point PointToImage(int x, int y, bool fitToBounds = true)
        {
            return PointToImage(new Point(x, y), fitToBounds);
        }

        /// <summary>
        ///   Converts the given client size point to represent a coordinate on the source image.
        /// </summary>
        /// <param name="point">The source point.</param>
        /// <param name="fitToBounds">
        ///   if set to <c>true</c> and the point is outside the bounds of the source image, it will be mapped to the nearest edge.
        /// </param>
        /// <returns><c>Point.Empty</c> if the point could not be matched to the source image, otherwise the new translated point</returns>
        public Point PointToImage(Point point, bool fitToBounds = true)
        {
            double x;
            double y;

            var viewport = GetImageViewPort();

            if (!!fitToBounds || viewport.Contains(point))
            {
                x = (point.X + Offset.X - viewport.X) / ZoomFactor;
                y = (point.Y + Offset.Y - viewport.Y) / ZoomFactor;

                var image = Image;
                if (image != null && fitToBounds)
                {
                    x = Math.Clamp(x, 0, image.Size.Width - 1);
                    y = Math.Clamp(y, 0, image.Size.Height - 1);
                }
            }
            else
            {
                x = 0; // Return Point.Empty if we couldn't match
                y = 0;
            }

            return new(x, y);
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.Point" /> repositioned to include the current image offset and scaled by the current zoom level
        /// </summary>
        /// <param name="source">The source <see cref="Point"/> to offset.</param>
        /// <returns>A <see cref="Point"/> which has been repositioned to match the current zoom level and image offset</returns>
        public Point GetOffsetPoint(System.Drawing.Point source)
        {
            var offset = GetOffsetPoint(new Point(source.X, source.Y));

            return new((int)offset.X, (int)offset.Y);
        }

        /// <summary>
        ///   Returns the source co-ordinates repositioned to include the current image offset and scaled by the current zoom level
        /// </summary>
        /// <param name="x">The source X co-ordinate.</param>
        /// <param name="y">The source Y co-ordinate.</param>
        /// <returns>A <see cref="Point"/> which has been repositioned to match the current zoom level and image offset</returns>
        public Point GetOffsetPoint(int x, int y)
        {
            return GetOffsetPoint(new System.Drawing.Point(x, y));
        }

        /// <summary>
        ///   Returns the source co-ordinates repositioned to include the current image offset and scaled by the current zoom level
        /// </summary>
        /// <param name="x">The source X co-ordinate.</param>
        /// <param name="y">The source Y co-ordinate.</param>
        /// <returns>A <see cref="Point"/> which has been repositioned to match the current zoom level and image offset</returns>
        public Point GetOffsetPoint(double x, double y)
        {
            return GetOffsetPoint(new Point(x, y));
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.PointF" /> repositioned to include the current image offset and scaled by the current zoom level
        /// </summary>
        /// <param name="source">The source <see cref="PointF"/> to offset.</param>
        /// <returns>A <see cref="PointF"/> which has been repositioned to match the current zoom level and image offset</returns>
        public Point GetOffsetPoint(Point source)
        {
            Rect viewport = GetImageViewPort();
            var scaled = GetScaledPoint(source);
            var offsetX = viewport.Left + Offset.X;
            var offsetY = viewport.Top + Offset.Y;

            return new(scaled.X + offsetX, scaled.Y + offsetY);
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.RectangleF" /> scaled according to the current zoom level and repositioned to include the current image offset
        /// </summary>
        /// <param name="source">The source <see cref="RectangleF"/> to offset.</param>
        /// <returns>A <see cref="RectangleF"/> which has been resized and repositioned to match the current zoom level and image offset</returns>
        public Rect GetOffsetRectangle(Rect source)
        {
            var viewport = GetImageViewPort();
            var scaled = GetScaledRectangle(source);
            var offsetX = viewport.Left - Offset.X;
            var offsetY = viewport.Top - Offset.Y;

            return new(new Point(scaled.Left + offsetX, scaled.Top + offsetY), scaled.Size);
        }

        /// <summary>
        ///   Returns the source rectangle scaled according to the current zoom level and repositioned to include the current image offset
        /// </summary>
        /// <param name="x">The X co-ordinate of the source rectangle.</param>
        /// <param name="y">The Y co-ordinate of the source rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <returns>A <see cref="Rectangle"/> which has been resized and repositioned to match the current zoom level and image offset</returns>
        public Rectangle GetOffsetRectangle(int x, int y, int width, int height)
        {
            return GetOffsetRectangle(new Rectangle(x, y, width, height));
        }

        /// <summary>
        ///   Returns the source rectangle scaled according to the current zoom level and repositioned to include the current image offset
        /// </summary>
        /// <param name="x">The X co-ordinate of the source rectangle.</param>
        /// <param name="y">The Y co-ordinate of the source rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <returns>A <see cref="RectangleF"/> which has been resized and repositioned to match the current zoom level and image offset</returns>
        public Rect GetOffsetRectangle(double x, double y, double width, double height)
        {
            return GetOffsetRectangle(new Rect(x, y, width, height));
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.Rectangle" /> scaled according to the current zoom level and repositioned to include the current image offset
        /// </summary>
        /// <param name="source">The source <see cref="Rectangle"/> to offset.</param>
        /// <returns>A <see cref="Rectangle"/> which has been resized and repositioned to match the current zoom level and image offset</returns>
        public Rectangle GetOffsetRectangle(Rectangle source)
        {
            var viewport = GetImageViewPort();
            var scaled = GetScaledRectangle(source);
            var offsetX = viewport.Left + Offset.X;
            var offsetY = viewport.Top + Offset.Y;

            return new(new System.Drawing.Point((int)(scaled.Left + offsetX), (int)(scaled.Top + offsetY)), new System.Drawing.Size((int)scaled.Size.Width, (int)scaled.Size.Height));
        }

        /// <summary>
        ///   Fits a given <see cref="T:System.Drawing.Rectangle" /> to match image boundaries
        /// </summary>
        /// <param name="rectangle">The rectangle.</param>
        /// <returns>
        ///   A <see cref="T:System.Drawing.Rectangle" /> structure remapped to fit the image boundaries
        /// </returns>
        public Rectangle FitRectangle(Rectangle rectangle)
        {
            var image = Image;
            if (image is null) return Rectangle.Empty;
            var x = rectangle.X;
            var y = rectangle.Y;
            var w = rectangle.Width;
            var h = rectangle.Height;

            if (x < 0)
            {
                x = 0;
            }

            if (y < 0)
            {
                y = 0;
            }

            if (x + w > image.Size.Width)
            {
                w = (int)(image.Size.Width - x);
            }

            if (y + h > image.Size.Height)
            {
                h = (int)(image.Size.Height - y);
            }

            return new(x, y, w, h);
        }

        /// <summary>
        ///   Fits a given <see cref="T:System.Drawing.RectangleF" /> to match image boundaries
        /// </summary>
        /// <param name="rectangle">The rectangle.</param>
        /// <returns>
        ///   A <see cref="T:System.Drawing.RectangleF" /> structure remapped to fit the image boundaries
        /// </returns>
        public Rect FitRectangle(Rect rectangle)
        {
            var image = Image;
            if (image is null) return Rect.Empty;
            var x = rectangle.X;
            var y = rectangle.Y;
            var w = rectangle.Width;
            var h = rectangle.Height;

            if (x < 0)
            {
                w -= -x;
                x = 0;
            }

            if (y < 0)
            {
                h -= -y;
                y = 0;
            }

            if (x + w > image.Size.Width)
            {
                w = image.Size.Width - x;
            }

            if (y + h > image.Size.Height)
            {
                h = image.Size.Height - y;
            }

            return new(x, y, w, h);
        }
        #endregion

        #region Navigate / Scroll methods
        /// <summary>
        ///   Scrolls the control to the given point in the image, offset at the specified display point
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to scroll to.</param>
        /// <param name="y">The Y co-ordinate of the point to scroll to.</param>
        /// <param name="relativeX">The X co-ordinate relative to the <c>x</c> parameter.</param>
        /// <param name="relativeY">The Y co-ordinate relative to the <c>y</c> parameter.</param>
        public void ScrollTo(double x, double y, double relativeX, double relativeY)
            => ScrollTo(new Point(x, y), new Point(relativeX, relativeY));

        /// <summary>
        ///   Scrolls the control to the given point in the image, offset at the specified display point
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to scroll to.</param>
        /// <param name="y">The Y co-ordinate of the point to scroll to.</param>
        /// <param name="relativeX">The X co-ordinate relative to the <c>x</c> parameter.</param>
        /// <param name="relativeY">The Y co-ordinate relative to the <c>y</c> parameter.</param>
        public void ScrollTo(int x, int y, int relativeX, int relativeY)
            => ScrollTo(new Point(x, y), new Point(relativeX, relativeY));

        /// <summary>
        ///   Scrolls the control to the given point in the image, offset at the specified display point
        /// </summary>
        /// <param name="imageLocation">The point of the image to attempt to scroll to.</param>
        /// <param name="relativeDisplayPoint">The relative display point to offset scrolling by.</param>
        public void ScrollTo(Point imageLocation, Point relativeDisplayPoint)
        {
            //CanRender = false;
            var zoomFactor = ZoomFactor;
            var x = imageLocation.X * zoomFactor - relativeDisplayPoint.X;
            var y = imageLocation.Y * zoomFactor - relativeDisplayPoint.Y;


            _canRender = true;
            Offset = new Vector(x, y);

            /*Debug.WriteLine(
                $"X/Y: {x},{y} | \n" +
                $"Offset: {Offset} | \n" +
                $"ZoomFactor: {ZoomFactor} | \n" +
                $"Image Location: {imageLocation}\n" +
                $"MAX: {HorizontalScrollBar.Maximum},{VerticalScrollBar.Maximum} \n" +
                $"ViewPort: {Viewport.Width},{Viewport.Height} \n" +
                $"Container: {HorizontalScrollBar.ViewportSize},{VerticalScrollBar.ViewportSize} \n" +
                $"Relative: {relativeDisplayPoint}");*/
        }

        /// <summary>
        ///   Centers the given point in the image in the center of the control
        /// </summary>
        /// <param name="imageLocation">The point of the image to attempt to center.</param>
        public void CenterAt(System.Drawing.Point imageLocation)
            => ScrollTo(new Point(imageLocation.X, imageLocation.Y), new Point(ViewPortSize.Width / 2, ViewPortSize.Height / 2));

        /// <summary>
        ///   Centers the given point in the image in the center of the control
        /// </summary>
        /// <param name="imageLocation">The point of the image to attempt to center.</param>
        public void CenterAt(Point imageLocation)
            => ScrollTo(imageLocation, new Point(ViewPortSize.Width / 2, ViewPortSize.Height / 2));

        /// <summary>
        ///   Centers the given point in the image in the center of the control
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to center.</param>
        /// <param name="y">The Y co-ordinate of the point to center.</param>
        public void CenterAt(int x, int y)
            => CenterAt(new Point(x, y));

        /// <summary>
        ///   Centers the given point in the image in the center of the control
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to center.</param>
        /// <param name="y">The Y co-ordinate of the point to center.</param>
        public void CenterAt(double x, double y)
            => CenterAt(new Point(x, y));

        /// <summary>
        /// Resets the viewport to show the center of the image.
        /// </summary>
        public void CenterToImage()
        {
            Offset = new Vector(HorizontalScrollBar.Maximum / 2, VerticalScrollBar.Maximum / 2);
        }
        #endregion

        #region Selection / ROI methods

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.Point" /> scaled according to the current zoom level
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to scale.</param>
        /// <param name="y">The Y co-ordinate of the point to scale.</param>
        /// <returns>A <see cref="Point"/> which has been scaled to match the current zoom level</returns>
        public Point GetScaledPoint(int x, int y)
        {
            return GetScaledPoint(new Point(x, y));
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.Point" /> scaled according to the current zoom level
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to scale.</param>
        /// <param name="y">The Y co-ordinate of the point to scale.</param>
        /// <returns>A <see cref="Point"/> which has been scaled to match the current zoom level</returns>
        public PointF GetScaledPoint(float x, float y)
        {
            return GetScaledPoint(new PointF(x, y));
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.Point" /> scaled according to the current zoom level
        /// </summary>
        /// <param name="source">The source <see cref="Point"/> to scale.</param>
        /// <returns>A <see cref="Point"/> which has been scaled to match the current zoom level</returns>
        public Point GetScaledPoint(Point source)
        {
            return new(source.X * ZoomFactor, source.Y * ZoomFactor);
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.PointF" /> scaled according to the current zoom level
        /// </summary>
        /// <param name="source">The source <see cref="PointF"/> to scale.</param>
        /// <returns>A <see cref="PointF"/> which has been scaled to match the current zoom level</returns>
        public PointF GetScaledPoint(PointF source)
        {
            return new((float)(source.X * ZoomFactor), (float)(source.Y * ZoomFactor));
        }

        /// <summary>
        ///   Returns the source rectangle scaled according to the current zoom level
        /// </summary>
        /// <param name="x">The X co-ordinate of the source rectangle.</param>
        /// <param name="y">The Y co-ordinate of the source rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <returns>A <see cref="Rectangle"/> which has been scaled to match the current zoom level</returns>
        public Rect GetScaledRectangle(int x, int y, int width, int height)
        {
            return GetScaledRectangle(new Rect(x, y, width, height));
        }

        /// <summary>
        ///   Returns the source rectangle scaled according to the current zoom level
        /// </summary>
        /// <param name="x">The X co-ordinate of the source rectangle.</param>
        /// <param name="y">The Y co-ordinate of the source rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <returns>A <see cref="RectangleF"/> which has been scaled to match the current zoom level</returns>
        public RectangleF GetScaledRectangle(float x, float y, float width, float height)
        {
            return GetScaledRectangle(new RectangleF(x, y, width, height));
        }

        /// <summary>
        ///   Returns the source rectangle scaled according to the current zoom level
        /// </summary>
        /// <param name="location">The location of the source rectangle.</param>
        /// <param name="size">The size of the source rectangle.</param>
        /// <returns>A <see cref="Rectangle"/> which has been scaled to match the current zoom level</returns>
        public Rect GetScaledRectangle(Point location, Size size)
        {
            return GetScaledRectangle(new Rect(location, size));
        }

        /// <summary>
        ///   Returns the source rectangle scaled according to the current zoom level
        /// </summary>
        /// <param name="location">The location of the source rectangle.</param>
        /// <param name="size">The size of the source rectangle.</param>
        /// <returns>A <see cref="Rectangle"/> which has been scaled to match the current zoom level</returns>
        public RectangleF GetScaledRectangle(PointF location, SizeF size)
        {
            return GetScaledRectangle(new RectangleF(location, size));
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.Rectangle" /> scaled according to the current zoom level
        /// </summary>
        /// <param name="source">The source <see cref="Rectangle"/> to scale.</param>
        /// <returns>A <see cref="Rectangle"/> which has been scaled to match the current zoom level</returns>
        public Rect GetScaledRectangle(Rect source)
        {
            return new(source.Left * ZoomFactor, source.Top * ZoomFactor, source.Width * ZoomFactor, source.Height * ZoomFactor);
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.RectangleF" /> scaled according to the current zoom level
        /// </summary>
        /// <param name="source">The source <see cref="RectangleF"/> to scale.</param>
        /// <returns>A <see cref="RectangleF"/> which has been scaled to match the current zoom level</returns>
        public RectangleF GetScaledRectangle(RectangleF source)
        {
            return new((float)(source.Left * ZoomFactor), (float)(source.Top * ZoomFactor), (float)(source.Width * ZoomFactor), (float)(source.Height * ZoomFactor));
        }

        /// <summary>
        ///   Returns the source size scaled according to the current zoom level
        /// </summary>
        /// <param name="width">The width of the size to scale.</param>
        /// <param name="height">The height of the size to scale.</param>
        /// <returns>A <see cref="SizeF"/> which has been resized to match the current zoom level</returns>
        public SizeF GetScaledSize(float width, float height)
        {
            return GetScaledSize(new SizeF(width, height));
        }

        /// <summary>
        ///   Returns the source size scaled according to the current zoom level
        /// </summary>
        /// <param name="width">The width of the size to scale.</param>
        /// <param name="height">The height of the size to scale.</param>
        /// <returns>A <see cref="Size"/> which has been resized to match the current zoom level</returns>
        public Size GetScaledSize(int width, int height)
        {
            return GetScaledSize(new Size(width, height));
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.SizeF" /> scaled according to the current zoom level
        /// </summary>
        /// <param name="source">The source <see cref="SizeF"/> to scale.</param>
        /// <returns>A <see cref="SizeF"/> which has been resized to match the current zoom level</returns>
        public SizeF GetScaledSize(SizeF source)
        {
            return new((float)(source.Width * ZoomFactor), (float)(source.Height * ZoomFactor));
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.Size" /> scaled according to the current zoom level
        /// </summary>
        /// <param name="source">The source <see cref="Size"/> to scale.</param>
        /// <returns>A <see cref="Size"/> which has been resized to match the current zoom level</returns>
        public Size GetScaledSize(Size source)
        {
            return new(source.Width * ZoomFactor, source.Height * ZoomFactor);
        }

        /// <summary>
        ///   Creates a selection region which encompasses the entire image
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown if no image is currently set</exception>
        public void SelectAll()
        {
            var image = Image;
            if (image is null) return;
            SelectionRegion = new Rect(0, 0, image.Size.Width, image.Size.Height);
        }

        /// <summary>
        /// Clears any existing selection region
        /// </summary>
        public void SelectNone()
        {
            SelectionRegion = Rect.Empty;
        }

        #endregion

        #region Viewport and image region methods
        /// <summary>
        ///   Gets the source image region.
        /// </summary>
        /// <returns></returns>
        public Rect GetSourceImageRegion(double mipScaleFactor = 1.0)
        {
            var image = Image;
            if (image is null) return Rect.Empty;

            switch (SizeMode)
            {
                case SizeModes.Normal:
                    var offset = Offset;
                    var viewPort = GetImageViewPort();
                    var zoomFactor = ZoomFactor;
                    double sourceLeft = (offset.X / zoomFactor) / mipScaleFactor;
                    double sourceTop = (offset.Y / zoomFactor) / mipScaleFactor;
                    double sourceWidth = (viewPort.Width / zoomFactor) / mipScaleFactor;
                    double sourceHeight = (viewPort.Height / zoomFactor) / mipScaleFactor;

                    return new(sourceLeft, sourceTop, sourceWidth, sourceHeight);
            }

            return new(0, 0, image.Size.Width, image.Size.Height);

        }

        /// <summary>
        /// Gets the image view port.
        /// </summary>
        /// <returns></returns>
        public Rect GetImageViewPort()
        {
            var viewPortSize = ViewPortSize;
            if (!IsImageLoaded || viewPortSize is { Width: 0, Height: 0 }) return Rect.Empty;

            double xOffset = 0;
            double yOffset = 0;
            double width = 0;
            double height = 0;

            switch (SizeMode)
            {
                case SizeModes.Normal:
                    if (AutoCenter)
                    {
                        xOffset = (!IsHorizontalBarVisible ? (viewPortSize.Width - ScaledImageWidth) / 2 : 0);
                        yOffset = (!IsVerticalBarVisible ? (viewPortSize.Height - ScaledImageHeight) / 2 : 0);
                    }

                    width = Math.Min(ScaledImageWidth - Math.Abs(Offset.X), viewPortSize.Width);
                    height = Math.Min(ScaledImageHeight - Math.Abs(Offset.Y), viewPortSize.Height);
                    break;
                case SizeModes.Stretch:
                    width = viewPortSize.Width;
                    height = viewPortSize.Height;
                    break;
                case SizeModes.Fit:
                    var image = Image;
                    double scaleFactor = Math.Min(viewPortSize.Width / image!.Size.Width, viewPortSize.Height / image.Size.Height);

                    width = Math.Floor(image.Size.Width * scaleFactor);
                    height = Math.Floor(image.Size.Height * scaleFactor);

                    if (AutoCenter)
                    {
                        xOffset = (viewPortSize.Width - width) / 2;
                        yOffset = (viewPortSize.Height - height) / 2;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(SizeMode), SizeMode, null);
            }

            return new(xOffset, yOffset, width, height);
        }
        #endregion

        #region Image methods
        public void LoadImage(string path)
        {
            Image = new Bitmap(path);
        }

        public Bitmap? GetSelectedBitmap()
        {
            var image = ImageAsWriteableBitmap;
            if (image is null || !HaveSelection) return null;

            var selection = SelectionRegionNet;
            var pixelSize = SelectionPixelSize;
            using var frameBuffer = image.Lock();

            var newBitmap = new WriteableBitmap(pixelSize, image.Dpi, frameBuffer.Format, AlphaFormat.Unpremul);
            using var newFrameBuffer = newBitmap.Lock();

            int i = 0;

            unsafe
            {
                var inputPixels = (uint*)(void*)frameBuffer.Address;
                var targetPixels = (uint*)(void*)newFrameBuffer.Address;

                for (int y = selection.Y; y < selection.Bottom; y++)
                {
                    var thisY = y * frameBuffer.Size.Width;
                    for (int x = selection.X; x < selection.Right; x++)
                    {
                        targetPixels![i++] = inputPixels![thisY + x];
                    }
                }
            }

            return newBitmap;
        }

        public Bitmap? CreateNewImageWithLayersFromSelection(PixelSize? targetSize = null)
        {
            return CreateNewImageWithLayersFromRegion(SelectionRegion, targetSize);
        }

        public Bitmap? CreateNewImageWithLayersFromRegion(Rect? region = null, PixelSize? targetSize = null)
        {
            if (Image != null)
            {
                var finalRegion = region ?? new Rect(0, 0, Image.PixelSize.Width, Image.PixelSize.Height);
                var finalSize = targetSize ?? new PixelSize(Convert.ToInt32(finalRegion.Width), Convert.ToInt32(finalRegion.Height));
                var newImage = new RenderTargetBitmap(finalSize);
                var layers = GetImageLayers(Image);
                using (var drawingContext = newImage.CreateDrawingContext(null))
                {
                    var dc = new DrawingContext(drawingContext);
                    dc.DrawImage(Image, finalRegion, new Rect(0, 0, finalSize.Width, finalSize.Height), Avalonia.Visuals.Media.Imaging.BitmapInterpolationMode.HighQuality);
                    if (layers != null)
                    {
                        foreach (var paintLayer in layers)
                        {
                            dc.DrawImage(paintLayer, finalRegion, new Rect(0, 0, finalSize.Width, finalSize.Height), Avalonia.Visuals.Media.Imaging.BitmapInterpolationMode.HighQuality);
                        }
                    }
                }

                return newImage;
            }

            return null;
        }

        public Bitmap? CreateNewImageFromMask()
        {
            if (Image != null)
            {
                var finalRegion = new Rect(0, 0, Image.PixelSize.Width, Image.PixelSize.Height);
                var finalSize = new PixelSize(Convert.ToInt32(finalRegion.Width), Convert.ToInt32(finalRegion.Height));
                var newImage = new RenderTargetBitmap(finalSize);
                var layers = GetImageMaskLayers(Image);
                using (var drawingContext = newImage.CreateDrawingContext(null))
                {
                    drawingContext.Clear(new Color(255, 255, 255, 255));
                    var dc = new DrawingContext(drawingContext);

                    if (layers != null)
                    {
                        foreach (var maskLayer in layers)
                        {
                            dc.DrawImage(maskLayer, finalRegion, new Rect(0, 0, finalSize.Width, finalSize.Height), Avalonia.Visuals.Media.Imaging.BitmapInterpolationMode.HighQuality);
                        }
                    }
                }
                return newImage;
            }

            return null;
        }
        #endregion

    }
}