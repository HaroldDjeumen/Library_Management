using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Librarymanage
{
    // Ensure the 'Book' class is defined within the namespace or imported from another namespace  
    public class Book
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string ReleaseDate { get; set; }
        public string ISBN { get; set; }
        public string ImagePath { get; set; }
        public string Summary { get; set; }
    }

    public partial class Library_Page : Page
    {
        private Frame _mainFrame;
        public Library_Page(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;

            LoadBooks();
        }

        private void LoadBooks()
        {
            List<Book> books = new List<Book>();

            if (File.Exists("books.txt"))
            {
                var lines = File.ReadAllLines("books.txt");

                foreach (var line in lines)
                {
                    var parts = line.Split(';');

                    books.Add(new Book
                    {
                        Title = parts[0],
                        Author = parts[1],
                        ReleaseDate = parts[2],
                        ISBN = parts[3],
                        ImagePath = parts[4],
                        Summary = parts[5]
                    });
                }
            }

            foreach (var book in books)
            {
                StackPanel bookPanel = new StackPanel
                {
                    Width = 150,
                    Margin = new Thickness(10),
                    Cursor = Cursors.Hand
                };

                Image cover = new Image
                {
                    Source = new BitmapImage(new Uri(book.ImagePath, UriKind.RelativeOrAbsolute)),
                    Width = 150,
                    Height = 200
                };

                TextBlock title = new TextBlock
                {
                    Text = book.Title,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center
                };

                bookPanel.Children.Add(cover);
                bookPanel.Children.Add(title);

                bookPanel.MouseLeftButtonUp += (s, e) => ShowBookDetails(book);

                BooksPanel.Children.Add(bookPanel);
            }
        }

        private void ShowBookDetails(Book book)
        {
            BookDetailsPanel.Visibility = Visibility.Visible;

            Image cover = new Image
            {
                Source = new BitmapImage(new Uri(book.ImagePath, UriKind.RelativeOrAbsolute)),
                Width = 200,
                Height = 250
            };

            BookTitle.Text =    +.Title;
            BookAuthor.Text = "Author: " + book.Author;
            BookReleaseDate.Text = "Released: " + book.ReleaseDate;
            BookISBN.Text = "ISBN: " + book.ISBN;
            BookSummary.Text = book.Summary;

            ;
        }
    }
}
