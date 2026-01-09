using Newtonsoft.Json;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Librarymanage
{
    // Ensure the 'Book' class is defined within the namespace or imported from another namespace  
    public class Book
    {
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? ReleaseDate { get; set; }
        public string? ISBN { get; set; }
        public string? ImagePath { get; set; }
        public string? Summary { get; set; }
        public BitmapImage ImageBitmap { get; set; }
        public string? PreviewUrl { get; set; }

    }


    public class Reservation
    {
        public int ReservationId { get; set; }
        public string? Username { get; set; }
        public string? BookTitle { get; set; }
        public string? ReservationDate { get; set; }
        public string? ReturnDate { get; set; }
    }

    public partial class Library_Page : Page
    {
        private Frame _mainFrame;
        private string _currentUsername;
        private Book _selectedBook;
        private string _currentBookTitle;
        private Reservation _selectedReservation;
        private static string connectionString = $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Library.db")};Version=3;";



#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public Library_Page(Frame mainFrame, string username)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            InitializeComponent();
            _mainFrame = mainFrame;
            _currentUsername = username;
            AccountName.Text = username;

            LoadBooks();
            BooksPanel.Visibility = Visibility.Visible;
           
            Loaded += async (s, e) =>
{
    await PreviewBrowser.EnsureCoreWebView2Async(null);
    PreviewBrowser.Source = new Uri("https://www.google.com");
};

            
            if (_selectedBook != null)
            {
                ReserveBook(_selectedBook.Title);
            }
        }

        private async Task LoadBooks(string searchQuery = "")
        {
            BooksPanel.Children.Clear();

            List<Book> books = LoadBooksFromDatabase(searchQuery);

            var tasks = books.Select(async book =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    StackPanel bookPanel = new StackPanel
                    {
                        Width = 132,
                        Margin = new Thickness(2,5,5,5),
                        Cursor = Cursors.Hand
                    };

                    Image cover = new Image
                    {
                        Width = 120,
                        Height = 190
                    };

                    if (book.ImageBitmap != null)
                    {
                        cover.Source = book.ImageBitmap;
                    }
                    else
                    {
                        // Use default image if book has no image
                        cover.Source = new BitmapImage(new Uri("Images/default-book.png", UriKind.Relative));
                    }

                    TextBlock title = new TextBlock
                    {
                        Text = book.Title,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 12,
                        Width = 120
                    };

                    bookPanel.Children.Add(cover);
                    bookPanel.Children.Add(title);
                    bookPanel.MouseLeftButtonUp += (s, e) => ShowBookDetails(book);

                    BooksPanel.Children.Add(bookPanel);
                });
            });

            await Task.WhenAll(tasks);
        }



        private List<Book> LoadBooksFromDatabase(string query = "")
        {
            List<Book> books = new List<Book>();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string sql = string.IsNullOrEmpty(query)
                    ? "SELECT * FROM Books"
                    : "SELECT * FROM Books WHERE Name LIKE @query OR Author LIKE @query";

                using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                {
                    if (!string.IsNullOrEmpty(query))
                    {
                        command.Parameters.AddWithValue("@query", "%" + query + "%");
                    }

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            byte[] imageBytes = reader["BookImage"] as byte[];

                            BitmapImage bookImage = null;
                            if (imageBytes != null && imageBytes.Length > 0)
                            {
                                using (var stream = new MemoryStream(imageBytes))
                                {
                                    bookImage = new BitmapImage();
                                    bookImage.BeginInit();
                                    bookImage.CacheOption = BitmapCacheOption.OnLoad;
                                    bookImage.StreamSource = stream;
                                    bookImage.EndInit();
                                }
                            }

                            books.Add(new Book
                            {
                                Title = reader["Name"].ToString(),
                                Author = reader["Author"].ToString(),
                                ReleaseDate = reader["Release-Date"].ToString(), 
                                ISBN = reader["ISBN"].ToString(),
                                Summary = reader["Description"].ToString(),
                                ImageBitmap = bookImage,
                            });
                        }
                    }
                }
            }

            return books;
        }




        private void ShowBookDetails(Book book)
        {
            _selectedBook = book;

            BookDetailsPanel.Visibility = Visibility.Visible;

            BigCoverImage.Source = book.ImageBitmap;
            BookTitle.Text = book.Title;
            BookAuthor.Text = "Author: " + book.Author;
            BookReleaseDate.Text = "Released: " + book.ReleaseDate;
            BookISBN.Text = "ISBN: " + book.ISBN;
            BookSummary.Text = book.Summary;

            // Reset
            CalenderView.Visibility = Visibility.Collapsed;
            SelectedReservationDateText.Text = "";
            ReservationPolicyText.Visibility = Visibility.Collapsed;
            ConfirmReservationButton.Visibility = Visibility.Collapsed;

            // Check if book is already reserved
            ReserveBook(book.Title);
        }

        private void ReserveBook(string bookTitle)
        {
            if (IsBookReserved(bookTitle))
            {
                bookbutton.Visibility = Visibility.Collapsed;
                BookAvail.Text = "This book is currently reserved.";
                BookAvail.Visibility = Visibility.Visible;
            }
            else
            {
                bookbutton.Visibility = Visibility.Visible;
                BookAvail.Visibility = Visibility.Collapsed;
            }
        }



        private void OpenCalendarButton_Click(object sender, RoutedEventArgs e)
        {
            CalenderView.Visibility = Visibility.Visible;

            ReservationPolicyText.Visibility = Visibility.Visible;
            

            // Limit the selection dates
            ReservationCalendar.DisplayDateStart = DateTime.Now;
            ReservationCalendar.DisplayDateEnd = DateTime.Now.AddMonths(1);

            ReturnCalendar.DisplayDateStart = DateTime.Now.AddDays(1);
            ReturnCalendar.DisplayDateEnd = DateTime.Now.AddMonths(2);
        }

        private void ReservationCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateReservationInfo();
        }

        private void ReturnCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateReservationInfo();
        }

        private void UpdateReservationInfo()
        {
            if (ReservationCalendar.SelectedDate.HasValue && ReturnCalendar.SelectedDate.HasValue)
            {
                DateTime reservationDate = ReservationCalendar.SelectedDate.Value;
                DateTime returnDate = ReturnCalendar.SelectedDate.Value;

                if (reservationDate >= returnDate)
                {
                    MessageBox.Show("Return date must be after reservation date.");
                    return;
                }

                SelectedReservationDateText.Text = $"Reserved: {reservationDate:dd/MM/yyyy} | Return: {returnDate:dd/MM/yyyy}";
                ReservationPolicyText.Visibility = Visibility.Visible;
                ConfirmReservationButton.Visibility = Visibility.Visible;
            }
        }


        private void ConfirmReservationButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBook != null && ReservationCalendar.SelectedDate.HasValue && ReturnCalendar.SelectedDate.HasValue)
            {
                DateTime reservationDate = ReservationCalendar.SelectedDate.Value;
                DateTime returnDate = ReturnCalendar.SelectedDate.Value;

                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    string query = "INSERT INTO Reservations (Username, BookTitle, ReservationDate, ReturnDate) VALUES (@Username, @BookTitle, @ReservationDate, @ReturnDate)";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", _currentUsername);
                        command.Parameters.AddWithValue("@BookTitle", _selectedBook.Title);
                        command.Parameters.AddWithValue("@ReservationDate", reservationDate.ToString("yyyy-MM-dd"));
                        command.Parameters.AddWithValue("@ReturnDate", returnDate.ToString("yyyy-MM-dd"));

                        command.ExecuteNonQuery();
                    }
                }

                ConfirmReservationButton.Visibility = Visibility.Collapsed;
                CalenderView.Visibility = Visibility.Collapsed;
                bookbutton.Visibility = Visibility.Collapsed;
                ReservationPolicyText.Visibility = Visibility.Collapsed;
                BookAvail.Text = "This book is currently reserved";
                BookAvail.Visibility = Visibility.Visible;

               // MessageBox.Show($"Book '{_selectedBook.Title}' reserved by {_currentUsername} from {reservationDate:dd/MM/yyyy} to {returnDate:dd/MM/yyyy}.");
            }
        }


        private bool IsBookReserved(string bookTitle)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Reservations WHERE BookTitle = @BookTitle";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BookTitle", bookTitle);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DateTime returnDate = DateTime.Parse(reader["ReturnDate"].ToString());
                            if (returnDate >= DateTime.Now)
                            {
                                return true; // Book is still reserved
                            }
                        }
                    }
                }
            }

            return false; // Book is available
        }





        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string query = SearchBox.Text.Trim();

            if (!string.IsNullOrEmpty(query))
            {
                BooksPanel.Children.Clear(); // Clear old books

                List<Book> searchResults = LoadBooksFromDatabase(query);

                if (searchResults.Count == 0)
                {
                    MessageBox.Show("No books found.");
                    return;
                }

                foreach (var book in searchResults)
                {

                    StackPanel bookPanel = new StackPanel
                    {
                        Width = 132,
                        Margin = new Thickness(2, 5, 5, 5),
                        Cursor = Cursors.Hand
                    };

                    Image cover = new Image
                    {
                        Source = book.ImageBitmap,
                        Width = 120,
                        Height = 190
                    };

                    

                    TextBlock title = new TextBlock
                    {
                        Text = book.Title,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        Width = 120
                    };

                    bookPanel.Children.Add(cover);
                    bookPanel.Children.Add(title);

                    bookPanel.MouseLeftButtonUp += (s2, e2) => ShowBookDetails(book);

                    BooksPanel.Children.Add(bookPanel);
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _mainFrame.Navigate(new Login_Page(_mainFrame));
        }

        private void Accountdetail_clicked(object sender, RoutedEventArgs e)
        {
            ReservePanel.Visibility = Visibility.Collapsed;
            BooksPanel.Visibility = Visibility.Collapsed;
            PreviewPanel.Visibility = Visibility.Collapsed;
            AccountPanel.Visibility = Visibility.Visible; // <- Show Account Details
            LoadAccountPage(); // <- Load user details
        }

        private void Library_clicked(object sender, RoutedEventArgs e)
        {
            LoadBooks();
            ReservePanel.Visibility = Visibility.Collapsed;
            AccountPanel.Visibility = Visibility.Collapsed;
            PreviewPanel.Visibility = Visibility.Collapsed;
            BooksPanel.Visibility = Visibility.Visible; // <- Show books panel
        }

        private void EbookButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void LoadAccountPage()
        {
            AccountPanel.Children.Clear();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // --- USER DETAILS ---
                string userQuery = "SELECT * FROM Users WHERE Username = @Username";
                using (SQLiteCommand userCommand = new SQLiteCommand(userQuery, connection))
                {
                    userCommand.Parameters.AddWithValue("@Username", _currentUsername);

                    using (SQLiteDataReader reader = userCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Main container for account info
                            StackPanel profileContainer = new StackPanel
                            {
                                Orientation = Orientation.Horizontal,
                                Margin = new Thickness(10),
                                VerticalAlignment = VerticalAlignment.Center
                            };

                            // Circle border for profile image
                            Border profileCircle = new Border
                            {
                                Width = 160,
                                Height = 160,
                                BorderBrush = Brushes.Gray,
                                BorderThickness = new Thickness(2),
                                Margin = new Thickness(0, 0, 20, 0),
                                Background = Brushes.LightGray // fallback color if no image
                            };

                            Image profileImage = new Image
                            {
                                Stretch = Stretch.Fill
                            };

                            // Load profile picture if available in DB
                            if (reader["ProfilePicture"] != DBNull.Value)
                            {
                                byte[] imageData = (byte[])reader["ProfilePicture"];
                                profileImage.Source = LoadImage(imageData);
                            }

                            profileCircle.Child = profileImage;

                            // Right side: account info
                            StackPanel accountInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center};

                            accountInfo.Children.Add(new TextBlock
                            {
                                Text = reader["Username"].ToString(),
                                FontSize = 25,
                                FontWeight = FontWeights.Bold,
                                Margin = new Thickness(10, 0, 0, 0),
                                Foreground = Brushes.Purple
                            });

                            accountInfo.Children.Add(new TextBlock
                            {
                                Text = "Email: " + reader["Email"].ToString(),
                                FontSize = 18,
                                Margin = new Thickness(10, 10, 0, 0),
                                Foreground = Brushes.Black
                            });

                            accountInfo.Children.Add(new TextBlock
                            {
                                Text = "Phone Number: " + reader["Phone"].ToString(),
                                FontSize = 18,
                                Margin = new Thickness(10, 5, 0, 0),
                                Foreground = Brushes.Black
                            });

                            accountInfo.Children.Add(new TextBlock
                            {
                                Text = "Join Date: " + reader["JoinDate"].ToString(),
                                FontSize = 18,
                                Margin = new Thickness(10, 5, 0, 0),
                                Foreground = Brushes.Black
                            });

                            Button changePicBtn = new Button
                            {
                                Content = "Change Picture",
                                Width = 120,
                                Height = 30,
                                Margin = new Thickness(50, 5, 0, 0)
                            };

                            changePicBtn.Click += ChangeProfilePicture_Click;

                            accountInfo.Children.Add(changePicBtn);

                            profileContainer.Children.Add(profileCircle);
                            profileContainer.Children.Add(accountInfo);

                            AccountPanel.Children.Add(profileContainer);
                        }
                    }
                }

                string reservationQuery = "SELECT * FROM Reservations WHERE Username = @Username";
                using (SQLiteCommand reservationCommand = new SQLiteCommand(reservationQuery, connection))
                {
                    reservationCommand.Parameters.AddWithValue("@Username", _currentUsername);

                    using (SQLiteDataReader reader = reservationCommand.ExecuteReader())
                    {
                        // Add title for reserved books if any exist
                        if (reader.HasRows)
                        {
                            TextBlock reservedHeader = new TextBlock
                            {
                                Text = "Reserved Books",
                                FontSize = 18,
                                FontWeight = FontWeights.Bold,
                                Foreground = Brushes.DarkBlue,
                                HorizontalAlignment = HorizontalAlignment.Left,
                                Margin = new Thickness(10, 20, 0, 10)
                            };
                            AccountPanel.Children.Add(reservedHeader);
                        }

                        while (reader.Read())
                        {
                            string bookTitle = reader["BookTitle"].ToString();
                            string reservationDate = reader["ReservationDate"].ToString();
                            string returnDate = reader["ReturnDate"].ToString();

                            Border card = new Border
                            {
                                Background = Brushes.White,
                                BorderBrush = Brushes.Gray,
                                BorderThickness = new Thickness(1),
                                CornerRadius = new CornerRadius(5),
                                Margin = new Thickness(10, 5, 10, 5),
                                Padding = new Thickness(10),
                                Width = 530
                            };

                            StackPanel content = new StackPanel();

                            TextBlock bookInfo = new TextBlock
                            {
                                Text = $"Book: {bookTitle}\nReserved: {reservationDate}\nReturn: {returnDate}",
                                TextWrapping = TextWrapping.Wrap,
                                FontSize = 14,
                                Margin = new Thickness(0, 0, 0, 5)
                            };

                            Button request = new Button
                            {
                                Content = "Request Date Change",
                                Width = 190,
                                Height = 30,
                                Margin = new Thickness(300, 5, 0, 0)
                            };

                            content.Children.Add(bookInfo);
                            content.Children.Add(request);
                            card.Child = content;

                            AccountPanel.Children.Add(card);
                        }
                    }
                }
            }
        }

        private void ChangeProfilePicture_Click(object sender, RoutedEventArgs e)
        {
            // File picker for image
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Profile Picture",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (dlg.ShowDialog() == true)
            {
                // Read file as byte[]
                byte[] imageData = File.ReadAllBytes(dlg.FileName);

                // Save into database
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string query = "UPDATE Users SET ProfilePicture = @Image WHERE Username = @Username";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Image", imageData);
                        cmd.Parameters.AddWithValue("@Username", _currentUsername);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Refresh UI so the new image appears
                LoadAccountPage();
            }
        }

        private ImageSource LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
                return null; // No image saved

            using (var ms = new MemoryStream(imageData))
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze(); // Makes it cross-thread safe
                return image;
            }
        }


        private async void OpenPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedBook.Title))
            {
                MessageBox.Show("Error getting Book Title");
                return;
            }

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT Name, PreviewUrl FROM Books WHERE Name LIKE @Title LIMIT 1";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Title", "%" + _selectedBook.Title + "%");

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string foundTitle = reader["Name"].ToString();
                            string previewUrl = reader["PreviewUrl"].ToString();

                            if (!string.IsNullOrEmpty(previewUrl) && previewUrl != "no preview available")
                            {
                                BookTitle.Text = foundTitle;

                                await PreviewBrowser.EnsureCoreWebView2Async(null);

                                LinkOpener(previewUrl);
                            }
                            else
                            {
                                MessageBox.Show("This book does not have a preview.");
                                PreviewPanel.Visibility = Visibility.Collapsed;
                            }
                        }
                        else
                        {
                            MessageBox.Show("No book found with that title.");
                        }
                    }
                }
            }
        }

        private async void Readmangas_clicked(object sender, RoutedEventArgs e)
        {
           LinkOpener("https://mangadex.org");
        }


        private async void LinkOpener(String link) 
        {
            await PreviewBrowser.EnsureCoreWebView2Async(null);

            if (PreviewBrowser.CoreWebView2 != null)
            {
                PreviewBrowser.CoreWebView2.Navigate(link);
                PreviewPanel.Visibility = Visibility.Visible;
                ReservePanel.Visibility = Visibility.Collapsed;
                BooksPanel.Visibility = Visibility.Collapsed;
                AccountPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                MessageBox.Show("CoreWebView2 failed ❌");
            }

            PreviewBrowser.CoreWebView2.NavigationCompleted += async (s, ev) =>
            {
                await PreviewBrowser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                                            document.body.style.backgroundColor = 'white';
                                            document.body.style.color = 'black';
                                            document.body.innerHTML = '<h1 style=""color:blue;"">Hello from WebView2</h1>';
                                    ");

            };
        }
    }
}


        


