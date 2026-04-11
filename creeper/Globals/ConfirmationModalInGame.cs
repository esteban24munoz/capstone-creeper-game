using Godot;
using System;

public partial class ConfirmationModalInGame : Control
{
	// This variable will store whatever function we pass into the modal
	private Action _onConfirmAction;

	public override void _Ready()
	{
		GetNode<Button>("%yesButton").Pressed += OnYesPressed;
		GetNode<Button>("%noButton").Pressed += OnNoPressed;
	}

	// Call this from MenuOptions to prepare and show the modal
	public void Setup(string message, Action onConfirm)
	{
		GetNode<Label>("%MessageText").Text = message; 

		_onConfirmAction = onConfirm;
		Visible = true; // Show the modal
	}

	private void OnYesPressed()
	{
		_onConfirmAction?.Invoke(); // Execute the stored function
		Visible = false; // Hide the modal
	}

	private void OnNoPressed()
	{
		Visible = false; // Just hide the modal, do nothing else
	}
}
