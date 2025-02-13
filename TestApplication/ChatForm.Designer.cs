namespace TestApplication
{
    partial class ChatForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            uxEndButton = new Button();
            uxMessageInputBox = new TextBox();
            uxChatMessages = new ListBox();
            uxSendButton = new Button();
            SuspendLayout();
            // 
            // uxEndButton
            // 
            uxEndButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            uxEndButton.Location = new Point(379, 269);
            uxEndButton.Name = "uxEndButton";
            uxEndButton.Size = new Size(84, 30);
            uxEndButton.TabIndex = 0;
            uxEndButton.Text = "End";
            uxEndButton.UseVisualStyleBackColor = true;
            uxEndButton.Click += OnEndClicked;
            // 
            // uxMessageInputBox
            // 
            uxMessageInputBox.Location = new Point(63, 221);
            uxMessageInputBox.Name = "uxMessageInputBox";
            uxMessageInputBox.Size = new Size(264, 23);
            uxMessageInputBox.TabIndex = 1;
            uxMessageInputBox.KeyDown += OnMessageInputKeyDown;
            // 
            // uxChatMessages
            // 
            uxChatMessages.FormattingEnabled = true;
            uxChatMessages.ItemHeight = 15;
            uxChatMessages.Location = new Point(63, 31);
            uxChatMessages.Name = "uxChatMessages";
            uxChatMessages.Size = new Size(340, 184);
            uxChatMessages.TabIndex = 2;
            // 
            // uxSendButton
            // 
            uxSendButton.Location = new Point(333, 219);
            uxSendButton.Name = "uxSendButton";
            uxSendButton.Size = new Size(70, 25);
            uxSendButton.TabIndex = 3;
            uxSendButton.Text = "Send";
            uxSendButton.UseVisualStyleBackColor = true;
            uxSendButton.Click += OnSendClick;
            // 
            // ChatForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(484, 311);
            Controls.Add(uxMessageInputBox);
            Controls.Add(uxChatMessages);
            Controls.Add(uxSendButton);
            Controls.Add(uxEndButton);
            MaximumSize = new Size(500, 350);
            MinimumSize = new Size(500, 350);
            Name = "ChatForm";
            Text = "ChatForm";
            FormClosed += OnClosed;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button uxEndButton;
        private TextBox uxMessageInputBox;
        private ListBox uxChatMessages;
        private Button uxSendButton;
    }
}