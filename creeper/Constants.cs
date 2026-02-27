public static class Constants {
	public enum Player
	{
		Hero,
		Enemy,
		Draw,
		None
	}

	public static IPlayer HeroPlayer = new LocalPlayer();
	public static IPlayer EnemyPlayer = new LocalPlayer();
}
