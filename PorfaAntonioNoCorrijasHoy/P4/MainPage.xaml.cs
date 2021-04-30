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
            GameTimerSetUp();
        }

        /// <summary>
        /// Esto establecer el mando para que empiece a funcinar :)
        /// </summary>
        public void GameTimerSetUp(){
            gameTimer = new DispatcherTimer();
            //Llamamos a un callback 
            gameTimer.Tick += GameTimer_Tick;
            gameTimer.Interval = new TimeSpan(1000); //1 segundo subnormal
            gameTimer.Start();

        }


        void GameTimer_Tick(object sender, object e){
            LeeMando();
            //DetectaGestosMando();
            DeadZoneMando();
            AplMando();
            FeedBack();
        }

        private void LeeMando(){
            //No hay GamePads en la lista, no añade ninguno
            if(myGamepads.Count != 0){
                //Selecciona el principal como el primero de la lista
                mainGamepad = myGamepads[0];
                prereading = reading;
                //Prerading es el reading anterior. Al principio es NULL
                reading = mainGamepad.GetCurrentReading();
            }
        }

        private void checkDeadZoneMando(ref double gamePadRead){
            if (gamePadRead< 0.1) gamePadRead += 0.1;
            else if (gamePadRead > 0.1) gamePadRead -= 0.1;
            else gamePadRead = 0;
        }

        private void DeadZoneMando(){
            checkDeadZoneMando(ref reading.RightThumbstickX);
            checkDeadZoneMando(ref reading.RightThumbstickY);
        }

        private void FeedBack(){
            if(mainGamepad != null){
                //get the first gamepad
                mainGamepad = myGamepads[0];

                //Set Vibration to your wife
                if ((reading.RightThumbstickX != 0) | (reading.RightThumbstickY != 0)){
                    double X = reading.RightThumbstickX * reading.RightThumbstickX;
                    double Y = reading.RightThumbstickY * reading.RightThumbstickY;

                    if (X > Y) vibration.RightMotor = X;
                    else vibration.RightMotor = Y;
                }
                else vibration.RightMotor = 0;

                if ((reading.LeftTrigger != 0) | (reading.RightTrigger != 0)) {
                    if (reading.LeftTrigger > reading.RightTrigger)
                        vibration.LeftMotor = reading.LeftTrigger;
                    else vibration.LeftMotor = reading.RightTrigger;
                }
                else vibration.LeftMotor = 0;

                //copy vibration to mainGamePad
                mainGamepad.Vibration = vibration;
            }
        }

        private void AplMando(){
            if((mainGamepad != null) && (Sel>=0) && (ImagenC.FocusState != FocusState.Unfocused)){

                //Obtenemos los valores de la imagens
                int X = (int)Canvas.GetLeft(ImagenC);
                int Y = (int)Canvas.GetTop(ImagenC);
                int Angulo = ListaDrones[Sel].Angulo;

                //Movemos la imagen en funcion de la posicion del JoyStick
                X = (int)(X + 10 * reading.RightThumbstickX);
                Y = (int)(Y + 10 * reading.RightThumbstickX);
                //Rotamos la imagen en funcion del indice del Trigger
                Angulo = (int)(Angulo + 10 * reading.RightTrigger);
                Angulo = (int)(Angulo + 10 * reading.LeftTrigger);

                //Aplicamos Pos
                Canvas.SetLeft(ImagenC, X);
                Canvas.SetTop(ImagenC, Y);

                //Aplicamos Rotation
                ListaDrones[Sel].Angulo = Angulo;
                CompositeTransform transformation = new CompositeTransform();
                transformation.Rotation = Angulo;
                transformation.CenterX = 25;
                transformation.CenterY = 25;
                ImagenC.RenderTransform = transformation;
            }
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
                    case VirtualKey.GamepadX:
                        Angulo--;
                        move = true;
                        break;
                    case VirtualKey.GamepadLeftTrigger:
                        e.Handled = true;
                        move = true;
                        break;
                    case VirtualKey.E:
                    case VirtualKey.GamepadY:
                        Angulo++;
                        move = true;
                        break;
                    case VirtualKey.GamepadRightTrigger:
                        e.Handled = true;
                        move = true;
                        break;
                        //Abajo
                }


                if(false){
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