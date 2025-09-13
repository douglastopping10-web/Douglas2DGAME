using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Schema;
using static System.Net.Mime.MediaTypeNames;

namespace Test_2dScroler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
 
 /// Object
     /// └── DispatcherObject
        ///    └── DependencyObject
            ///      └── Visual
               ///        └── UIElement
                  ///               └── FrameworkElement
                     ///                    └── (many WPF controls, shapes, panels, etc.)

    public partial class MainWindow : Window
    {
        Dictionary<string, object> ObstacleData = new Dictionary<string, object>()
        {
            { "Width", 100.0 },
            { "Height", 100.0 },
            { "Left", 1500.0 },
            { "Top", 205.0},
            { "Source", new BitmapImage(new Uri("/Images/Spikes.png", UriKind.Relative)) },
            { "Frontenator", 10 }
        };
        private double speed = 4;

        public Random rng = new Random();
        private double jumpStrength =  -11;
        private double verticalVelocity = 0;
        private double bulletSpeed = 5;
        private double gravity = .25;
        private bool CameraRight = true;
        private bool invincible = false;
        private bool moveLeft = false;
        private bool moveRight = false;
        private bool GameRunning = false;
        private DispatcherTimer bulletCooldown;
        private bool CanShoot = true;
        private Dictionary<UIElement, double> defaultPositions = new Dictionary<UIElement, double>();
        private List<FrameworkElement> VisibleObstacles = new List<FrameworkElement>();
        private List<System.Windows.Controls.Image> Hearts = new List<System.Windows.Controls.Image>();

        public MainWindow()
        {
            InitializeComponent();
            CreateHearts(3);
            defaultPositions[Background3] = -Background3.Width;
            defaultPositions[Background] = 0;
            defaultPositions[Background2] = Background.Width;
            foreach (var bg in defaultPositions.Keys)
            {
                Canvas.SetLeft(bg, defaultPositions[bg]);
            }
            bulletCooldown = new DispatcherTimer();
            bulletCooldown.Interval = TimeSpan.FromMilliseconds(500);
            bulletCooldown.Tick += (s, e) => { CanShoot = true; bulletCooldown.Stop(); };


        }

        
        private void CreateHearts(int NumberOf)
        {
          
            for (int i = 0; i < NumberOf; i++)
            {
                if (Hearts.Count == 0)
                {
                    System.Windows.Controls.Image heart = new System.Windows.Controls.Image() { Height = 50, Width = 50, Source = new BitmapImage(new Uri("Images/Heart.png", UriKind.Relative))};
                    MovingCanvas.Children.Add(heart);
                    Canvas.SetLeft(heart, 25);
                    Canvas.SetTop(heart, 20);
                    Hearts.Add(heart);
                   
                }
                else
                {   
                    double lastHeartX = Canvas.GetLeft(Hearts[Hearts.Count - 1]);


                    System.Windows.Controls.Image heart = new System.Windows.Controls.Image() { 
                        Height = 50,
                        Width = 50,
                        Source = new BitmapImage(new Uri("Images/Heart.png", UriKind.Relative)) };

                    MovingCanvas.Children.Add(heart);
                    Canvas.SetLeft(heart, lastHeartX + 50);
                    Canvas.SetTop(heart, 20);
                    Hearts.Add(heart);
                }

            }

        }
        private void Start_Click(object sender, RoutedEventArgs e)
        {

            
            Start.Visibility = Visibility.Collapsed;
            MovingCanvas.Visibility = Visibility.Visible;
            if (!GameRunning)
            {
                GameRunning = true;
                MovingCanvas.Focus();
                CompositionTarget.Rendering += GameLoop;
            }

        }
        private void PauseGame()
        {
            GameRunning = false;
            CompositionTarget.Rendering -= GameLoop;
            bulletCooldown.Stop();
        }
        private void UnPauseGame()
        {
            GameRunning = true;
            CompositionTarget.Rendering += GameLoop;
            bulletCooldown.Start();
        }
        private void PauseGameDamage()
        {
            if (!invincible)
            { 
                if(Hearts.Count <= 0)
                {
                    GameOverScreen.Visibility = Visibility.Visible;
                    PauseGame();
                }
                if (Hearts.Count > 0)
                {
                    var lastHeart = Hearts[Hearts.Count - 1];
                    MovingCanvas.Children.Remove(lastHeart);
                    Hearts.RemoveAt(Hearts.Count - 1);
                }
                CompositionTarget.Rendering -= GameLoop;
                bulletCooldown.Stop();

                Player.Fill = Brushes.Red;
                invincible = true;

                DispatcherTimer DeathTimer = new DispatcherTimer();
                DeathTimer.Interval = TimeSpan.FromSeconds(1);
                var InvincibleTimer = new DispatcherTimer();
                InvincibleTimer.Interval = TimeSpan.FromSeconds(3);
                InvincibleTimer.Tick += (s, e) => { invincible = false; InvincibleTimer.Stop(); };
                InvincibleTimer.Start();
                DeathTimer.Tick += (s, e) =>
                {
                    DeathTimer.Stop();

                    // Restore player color
                    Player.Fill = Brushes.Blue;


                    // Resume game loop
                    bulletCooldown.Start();
                    CompositionTarget.Rendering += GameLoop;
                };
                DeathTimer.Start();
            }
        }
        private void PlayerMovingGravitinator(object sender, EventArgs e)
        {
            verticalVelocity += gravity;
            foreach(FrameworkElement ObstacleCheck in VisibleObstacles)
            {
               if(CheckCollision(Player, ObstacleCheck, 50))
               {
                    PauseGameDamage();
               }
            }
            double newTop = Canvas.GetTop(Player) + verticalVelocity;
            double newGunTop = Canvas.GetTop(PlayerGun) + verticalVelocity;

            double groundTop = Canvas.GetTop(Ground) - Player.Height;


            if (newTop >= groundTop)
            {
                // ON GROUND
                Canvas.SetTop(Player, groundTop);
                Canvas.SetTop(PlayerGun, groundTop + 20);
                verticalVelocity = 0;
            }
            else
            {
                // IN AIR
                Canvas.SetTop(Player, newTop);
                Canvas.SetTop(PlayerGun, newTop + 20);
            }
        }
        private void MoveBackgroundinator(object sender, EventArgs e)
        {
            foreach (var bg in defaultPositions.Keys)
            {
                foreach (var ObstacleMovinator in VisibleObstacles)
                {
                    double ObstacleSpeed = speed / 3;
                    Canvas.SetLeft(ObstacleMovinator, Canvas.GetLeft(ObstacleMovinator) - ObstacleSpeed);
                }
                Canvas.SetLeft(bg, Canvas.GetLeft(bg) - speed);
            }
            // Check if Bg2 or Bg3 right edge enters the main window
            double windowWidth = MovingCanvas.ActualWidth;

            bool reset = false;

            // Check right edge of Bg2
            double bg2RightEdge = Canvas.GetLeft(Background2) + Background2.ActualWidth;
            if (bg2RightEdge <= windowWidth && speed > 0) reset = true; // moving left

            // Check right edge of Bg3
            double bg3LeftEdge = Canvas.GetLeft(Background3);
            if (bg3LeftEdge >= 0 && speed < 0) reset = true; // moving right

            // put back in default positions
            if (reset)
            {
                foreach (var bg in defaultPositions.Keys)
                {
                    Canvas.SetLeft(bg, defaultPositions[bg]);
                }
                CreateObstacleWhenBGResets();
            }



        }  
        private void CreateObstacleWhenBGResets()
        {
           if(rng.NextDouble() <= .5)
           {
                System.Windows.Controls.Image Obstacle = new System.Windows.Controls.Image()
                {
                    Width = (double)ObstacleData["Width"],
                    Height = (double)ObstacleData["Height"],
                    Stretch = Stretch.Fill,
                    Source = (ImageSource)ObstacleData["Source"],      
                    
                };
                Canvas.SetLeft(Obstacle, (double)ObstacleData["Left"]);
                Canvas.SetTop(Obstacle, (double)ObstacleData["Top"]);
                Canvas.SetZIndex(Obstacle,(int)ObstacleData["Frontenator"]);
                MovingCanvas.Children.Add(Obstacle);
                VisibleObstacles.Add(Obstacle);       
            }
           
        }
        private void HandleCameraGunKindaThing(bool CameraisRight)
        {
            if (CameraisRight)
            {
                Canvas.SetLeft(PlayerGun, Canvas.GetLeft(Player) + 40);
                Canvas.SetTop(PlayerGun, Canvas.GetTop(Player) + 20);
            }
            else
            {
                Canvas.SetLeft(PlayerGun, Canvas.GetLeft(Player) - 40);
                Canvas.SetTop(PlayerGun, Canvas.GetTop(Player) + 20);
            }
        }
        private void GameLoop(object sender, EventArgs e)
        {
            // Background
            if (moveRight)
            {
                speed = 5;
                CameraRight = true;
                HandleCameraGunKindaThing(CameraRight);
                MoveBackgroundinator(sender, e);
                
            }
            if (moveLeft)
            {
                speed = -5;
                CameraRight = false;
                HandleCameraGunKindaThing(CameraRight);
                MoveBackgroundinator(sender, e);
            }
            if (Hearts.Count <= 0)
            {
                GameOverScreen.Visibility = Visibility.Visible;
                PauseGame();
            }



            PlayerMovingGravitinator(sender, e);
        }
        private bool CheckCollision(FrameworkElement PlayerBox, FrameworkElement BadThing, int NicenessFactor)
        {
            int heightfactor = NicenessFactor * 2;
            // Get positions
            double ax = Canvas.GetLeft(PlayerBox);
            double ay = Canvas.GetTop(PlayerBox);
            double bx = Canvas.GetLeft(BadThing);
            double by = Canvas.GetTop(BadThing);

            // Build bounding boxes
            Rect rectA = new Rect(ax, ay, PlayerBox.Width, PlayerBox.Height);
            Rect rectB = new Rect(bx, by, BadThing.Width - NicenessFactor, BadThing.Height - heightfactor);

            return rectA.IntersectsWith(rectB);
        }
        private void ShootGun()
        {   
            Ellipse Bullet = new Ellipse() { Width = 20, Height = 20, Fill = Brushes.Yellow };
            double left;
            if (CameraRight)
            {
                left = Canvas.GetLeft(PlayerGun) + PlayerGun.Width + 2;
            }
            else
            {
                left = Canvas.GetLeft(PlayerGun) + 2;
            }
            double top = Canvas.GetTop(PlayerGun) + PlayerGun.Height / 2 - Bullet.Height / 2;

            Canvas.SetLeft(Bullet, left);
            Canvas.SetTop(Bullet, top);
            
            MovingCanvas.Children.Add(Bullet);
            Rect BulletHitbox = new Rect(left, top, Bullet.Width, Bullet.Height);
            BulletFiring(Bullet, BulletHitbox, CameraRight);
        }
        private void BulletFiring(UIElement Bullet, Rect BulletHitbox, bool Right)
        {
            EventHandler handler = null;
            handler = (s, e) =>
            {
                double direction = Right ? 1 : -1; // 1 is if right comes as true -1 if right not true
                double currentX = Canvas.GetLeft(Bullet);
                Canvas.SetLeft(Bullet, currentX + direction * bulletSpeed);

                if (currentX > MovingCanvas.ActualWidth || currentX < 0)
                {
                    CompositionTarget.Rendering -= handler;
                    MovingCanvas.Children.Remove(Bullet);
                }

            };

            CompositionTarget.Rendering += handler; // attach the handler
        }


       
        private void Canvas_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.D)
            {
                moveRight = false;
            }
            if (e.Key == System.Windows.Input.Key.A)
            {
                moveLeft = false;
            }



        }

        private void Canvas_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && CanShoot)
            {
                ShootGun();
                CanShoot = false;
                bulletCooldown.Start();
            }
            if (e.Key == System.Windows.Input.Key.D)
            {
                moveRight = true;
            }
            if (e.Key == System.Windows.Input.Key.A)
            {
                moveLeft = true;
            }
            if (e.Key == System.Windows.Input.Key.Space && verticalVelocity == 0)
            {
                verticalVelocity = jumpStrength; // if space is currently down and player is not in the sky or under the floor then you start jumping
            }

        }

        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            GameOverScreen.Visibility = Visibility.Collapsed;
            UnPauseGame();
            CreateHearts(3);
            Start.Visibility = Visibility.Collapsed;
            MovingCanvas.Visibility = Visibility.Visible;
            MovingCanvas.Focus();
            for (int i = VisibleObstacles.Count - 1; i >= 0; i--)
            {
                var obstacle = VisibleObstacles[i];
                MovingCanvas.Children.Remove(obstacle);
                VisibleObstacles.RemoveAt(i);
            }
            foreach (var bg in defaultPositions.Keys)
            {
                Canvas.SetLeft(bg, defaultPositions[bg]);
            }
        }
    }
   
}