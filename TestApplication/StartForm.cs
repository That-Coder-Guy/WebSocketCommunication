
namespace TestApplication
{
    public partial class StartForm : Form
    {
        public string EnteredName = "";

        public StartForm()
        {
            InitializeComponent();
        }

        private void NameKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                EnteredName = uxNameTextBox.Text;
                Close();
            }
        }
    }
}
