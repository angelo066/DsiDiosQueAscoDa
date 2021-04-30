using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.Gaming.Input;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace P4
{
    /// <summary>
    /// Página vacía que se puede usar de forma independiente o a la que se puede navegar dentro de un objeto Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public ObservableCollection<VMDron> ListaDrones { get; } = new ObservableCollection<VMDron>();
        PointerPoint ptrPt;
        int Sel = -1;
        bool BotDer = false, BotIzq = false;

        CoreCursor pin;
        CoreCursor normal;

        //Controlar mandos

        private readonly object myLock = new object();
        private List<Gamepad> myGamepads = new List<Gamepad>();
        private Gamepad mainGamepad = null;
        private GamepadReading reading, prereading;
        private GamepadVibration vibration;

        //Manejar el timer
        DispatcherTimer gameTimer;


        public MainPage()
        {
            this.InitializeComponent();

            pin = new CoreCursor(CoreCursorType.Pin, 0);
            normal = new CoreCursor(CoreCursorType.Arrow, 0);


            Gamepad.GamepadAdded += (object sender, Gamepad e) =>
            {
                lock (myLock)
                {
                    bool gamepadInList = myGamepads.Contains(e);
                    //Mira se programar 
                    if (!gamepadInList) myGamepads.Add(e);
                }
            };


            Gamepad.GamepadRemoved += (object sender, Gamepad e) =>{
                lock (myLock){
                    //Buscamos el indice del GamePad que se ha removido
                    int indexRemoved = myGamepads.IndexOf(e);
                    // Si existe en la lista
                    if(indexRemoved > -1){
                        //Verificamos si es el actual /princiapl
                        if (mainGamepad == myGamepads[indexRemoved])
                            mainGamepad = null;
                        //Se remueve de la lista sea el principal o no
                        myGamepads.RemoveAt(indexRemoved);
                    }
                }
            };
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Cosntruye las listas de ModelView a partir de la lista Modelo 
            if (ListaDrones != null)
                foreach (Dron dron in Model.GetAllDrones())
                {
                    VMDron VMitem = new VMDron(dron);
                    ListaDrones.Add(VMitem);
                }
            base.OnNavigatedTo(e);
        }


        private void ImageGridView_ItemClick(object sender, ItemClickEventArgs e) {
            VMDron Item = e.ClickedItem as VMDron;
            Sel = Item.Id;

            //Declara la imagen que se ha seleccionado para mostrarla abajo a la derecha
            Imagen.Source = Item.Img.Source;
            //Pone el texto que describe a la imagen
            Texto.Text = Item.Explicacion;
            //Actualizamos la imagen que se va a poner encima del mapa
            ImagenC.Content = Item.Img;

            //ImagenC.
            Canvas.SetLeft(ImagenC, Item.X);
            Canvas.SetTop(ImagenC, Item.Y);

            CompositeT.Rotation = 0;
        }

        private void ImagenC_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint NewptrPt = e.GetCurrentPoint(MiCanvas);
            //float dX = (int)NewptrPt.Position.X - (int)ptrPt.Position.X;
            if ((Sel >= 0) && ((BotIzq || BotDer)))
            Window.Current.CoreWindow.PointerCursor = pin;
            {
                if (BotIzq) {

                    Canvas.SetLeft(ImagenC, (int)NewptrPt.Position.X - 25);
                    Canvas.SetTop(ImagenC, (int)NewptrPt.Position.Y - 25);
                    //ListaDrones[Sel].Transform.TranslateX = (int)NewptrPt.Position.X - 25;
                    //ListaDrones[Sel].Transform.TranslateY = (int)NewptrPt.Position.Y - 25;
                }

                if (BotDer)
                {
                    ListaDrones[Sel].Angulo = (int)NewptrPt.Position.X - (int)ptrPt.Position.X;
                    ListaDrones[Sel].Transform.Rotation = ListaDrones[Sel].Angulo;

                    ImagenC.RenderTransform = ListaDrones[Sel].Transform;

                    
                }

                //ListaDrones[Sel].Transform.TranslateY = ListaDrones[Sel].Y - 25;
                //ListaDrones[Sel].Transform.TranslateX = ListaDrones[Sel].X - 25;

                //ListaDrones[Sel].Transform.CenterX = 25;
                //ListaDrones[Sel].Transform.CenterY = 25;

            }

        }

        private void ImagenC_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint ptrPt = e.GetCurrentPoint(MiCanvas);
            if (!ptrPt.Properties.IsLeftButtonPressed) BotIzq = false;
            if (!ptrPt.Properties.IsRightButtonPressed) BotDer = false;

            Window.Current.CoreWindow.PointerCursor = normal;
        }

        private void CommandBar_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Tab){
                e.Handled = true;
                DependencyObject candidate = null;
                candidate = FocusManager.FindNextFocusableElement(FocusNavigationDirection.Down);
                (candidate as Control).Focus(FocusState.Keyboard);
            }
        }

        private void ImageC_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            bool move = false;
            if (Sel >= 0){
                //int X = ListaDrones[Sel].X;
                //int Y = ListaDrones[Sel].Y;

                int X = (int)Canvas.GetLeft(ImagenC);
                int Y = (int)Canvas.GetTop(ImagenC);

                int Angulo = ListaDrones[Sel].Angulo;


                switch (e.Key)
                {
                    //Izquierda
                    case VirtualKey.A:
                    case VirtualKey.GamepadRightThumbstickLeft:
                        X -= 10;
                        move = true;
                        break;
                        //Derecha
                    case VirtualKey.D:
                    case VirtualKey.GamepadRightThumbstickRight:
                        X += 10;
                        move = true;
                        break;
                        //Arriba
                    case VirtualKey.W:
                    case VirtualKey.GamepadRightThumbstickUp:
                        Y -=  10;
                        move = true;
                        break;
                    case VirtualKey.S:
                    case VirtualKey.GamepadRightThumbstickDown:
                        Y += 10;
                        move = true;
                        break;
                    case VirtualKey.Q:
                    case VirtualKey.GamepadLeftTrigger:
                        Angulo--;
                        move = true;
                        e.Handled = true;
                        break;
                    case VirtualKey.E:
                    case VirtualKey.GamepadRightTrigger:
                        Angulo++;
                        move = true;
                        e.Handled = true;
                        break;
                        //Abajo
                }


                if(move){
                    Canvas.SetLeft(ImagenC, (int)X);
                    Canvas.SetTop(ImagenC, (int)Y);

                    
                    ListaDrones[Sel].Angulo = Angulo;
                    CompositeTransform transformation = new CompositeTransform();
                    transformation.Rotation = Angulo;
                    transformation.CenterX = 25;
                    transformation.CenterY = 25;
                    ImagenC.RenderTransform = transformation;
                }
            }
        }

        private void ImagenC_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ptrPt = e.GetCurrentPoint(MiCanvas);
            if (ptrPt.Properties.IsLeftButtonPressed) BotIzq = true;
            //Establecer Cursor
            if (ptrPt.Properties.IsRightButtonPressed) BotDer = true;


        }
    }
}