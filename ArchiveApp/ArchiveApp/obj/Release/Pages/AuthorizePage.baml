   M S B A M L   `   `   `  ���� E  AArchiveApp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'clr-namespace:ArchiveApp
ArchiveApp      ArchiveApp.AuthorizePage   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     @d2http://schemas.microsoft.com/expression/blend/2008     *localclr-namespace:ArchiveApp     -     N� Title$  AuthorizePage�� N� 
Background
 �����.��� -   $��MainGrid��.+�� -   $��CaptchaContainer��$��Center=�$��Center=�$��	0,0,140,0q� K� ZIndex$ 2��.+��� -   $��CaptchaImage��$	��230��$��70��$��	0,0,0,150q��� -   $��CaptchaTextBox��$��Center=�$	��230��$��30��$��0,0,0,25q�$��Center=�$��Center=��� -   $��CaptchaText��$"��Введите капчу...��$ 2��$��
0,0,120,25q�$��Center=�$��Center=�$��14(� �� TextAlignment$
 Left=� J� IsHitTestVisible . �� -   $��RefreshCaptcha��
������ $��0q�$��16(�$��30��$��30��$��Center=�$��Center=�$��
200,0,0,25q�.���� $��/Resources/UpdateIcon.png���� -   $��CaptchaSubmitBtn��$��Проверить�����#bae3e8$��16(�$	��160��$��30��$��Center=�$��Center=�$��0,50,0,0q��� -	   $��LoginBox��$��Center=�$��Center=�$��Center=�$	��300��$��50��$��20(�$��0,0,140,120q�$��10,0,0,0q��� -
   $��	LoginText��$��
Логин��$��Center=�$��Center=�$
 Left=�$��20(�$��0,0,360,120q� . �� -   $��PasswordText��$��Пароль��$��Center=�$��Center=�$
 Left=�$��20(�$��	0,0,350,0q�$ 1�� . G� -   $��PasswordBox��$��Center=�$��Center=�$��Center=�$	��300��$��50��$��	0,0,140,0q�$��20(�$��10,0,0,0q��� -   $��LoginBtn��$��
Войти��$��Center=����#bae3e8$��Center=�$��Center=�$	��140��$��40��$��0,110,300,0q�$��18(��� -   $��	CancelBtn��$��Отмена��$��Center=����#bae3e8$��Center=�$��Center=�$	��140��$��40��$��
20,110,0,0q�$��18(��� -   $��ErrorMessage��$��Center=�$��Center=�$��0,160,140,0q�
���  ��$��14(�$ Center=�$y�	Collapsed=�$
��Wrap=�$	��300��