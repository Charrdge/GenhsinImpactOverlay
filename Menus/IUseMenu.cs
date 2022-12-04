namespace GenshinImpactOverlay.Menus;

internal interface IUseMenu
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="updateMenuFunc">Метод, который можно вызвать для динамического обновления меню</param>
	/// <param name="keyInputSwitchFunc">Метод, который можно вызвать чтобы заблокироать или разблокировать управление меню. Передайте <see langword="null"/> для инверсии статуса фокуса</param>
	/// <returns>Сформированное меню. Везвращает <see langword="null"/>, если меню добавлять не требуется</returns>
	public MenuItem? GetMenu(Action<MenuItem> updateMenuFunc, Action<bool?> keyInputSwitchFunc);
}