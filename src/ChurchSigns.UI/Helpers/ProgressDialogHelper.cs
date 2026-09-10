using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ChurchSigns.UI.Helpers
{
    public static class ProgressDialogHelper
    {
        /// <summary>
        /// Shows a progress dialog for a known number of steps.
        /// </summary>
        /// <param name="xamlRoot">The XamlRoot of the calling window.</param>
        /// <param name="title">Dialog title.</param>
        /// <param name="totalSteps">Total number of steps (e.g., images).</param>
        /// <param name="workAction">Async action to perform for each step. Receives step index and CancellationToken.</param>
        /// <returns>True if cancelled, false if completed.</returns>
        public static async Task<bool> ShowProgressDialogAsync(
            XamlRoot xamlRoot,
            string title,
            int totalSteps,
            Func<int, CancellationToken, Task> workAction)
        {
            if (totalSteps <= 0)
                throw new ArgumentException("Total steps must be greater than zero.", nameof(totalSteps));

            var cts = new CancellationTokenSource();
            bool cancelled = false;

            // UI elements
            var progressText = new TextBlock { Text = $"Step 0 of {totalSteps}" };
            var progressBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = totalSteps,
                Value = 0,
                Height = 20
            };

            // Create dialog
            var progressDialog = new ContentDialog
            {
                Title = title,
                PrimaryButtonText = "Cancel",
                CloseButtonText = string.Empty,
                DefaultButton = ContentDialogButton.None,
                Content = new StackPanel
                {
                    Spacing = 10,
                    Children =
                    {
                        progressText,
                        progressBar
                    }
                },
                XamlRoot = xamlRoot
            };

            // Cancel button handler
            progressDialog.PrimaryButtonClick += (s, args) =>
            {
                cts.Cancel();
                args.Cancel = true; // Keep dialog open until loop ends
            };

            // Show dialog
            var showTask = progressDialog.ShowAsync();

            try
            {
                for (int i = 1; i <= totalSteps; i++)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    // Perform the work for this step
                    await workAction(i, cts.Token);

                    // Update UI
                    progressBar.Value = i;
                    progressText.Text = $"Step {i} of {totalSteps}";
                }
            }
            catch (TaskCanceledException)
            {
                cancelled = true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{ex.GetType().Name} {ex.Message}");
                cancelled = false;
            }

            // Close dialog
            progressDialog.Hide();

            return cancelled;
        }
    }
}
