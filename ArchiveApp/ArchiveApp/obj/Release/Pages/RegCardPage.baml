   M S B A M L   `   `   `  ���� E  AArchiveApp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'clr-namespace:ArchiveApp
ArchiveApp      ArchiveApp.RegCardPage   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     @d2http://schemas.microsoft.com/expression/blend/2008     *localclr-namespace:ArchiveApp       N� Title$  RegCardPage�� � 	Resources     ArchiveApp.BoolToconverter   BoolToсonverter  EditButtonStyle �� 
TargetType �� Property �� Value �� 
TargetType �� Triggers P� Property P� Value��%�  &	        &	        �� " ��.���  � ��
 �   ���  � �� �� " ��.R��� #��z��#��z��#��z��._��� $��Center=�$��Center=� P�  � {� .  . ���  � �� �#a4d2d9��  � ��
 �   ���  � ��
 �   ���  � ��$	 0.5q�P�  � {� . . ���  � �� �#bae3e8��  � ��
 �   ���  � ��
 �   ���  � ��$	 0.5q�.��� -   $��MainGrid��$��10q�
�������.+��� -   	 ��Name$	 DocumentComboBox��$
��auto��$��25��$	��Top=�$
��Left=�$��0,0,0,0q�$��7,0,0,0q�
 �� DisplayMemberPath$
 Title�� �� SelectedValuePath$ Id��$��Center=��� ���#f3f3f3 �� CornerRadius$ 10�� �� Padding$ 20q�$	��400��$	��330��$��Center=�$��Center=����#cccccc$��1q�._��� .+��� $*��$Название документа:��$
��Bold"�$��0,0,0,5q��� -    ��Name$ TitleTextBox��$��30��$��0,0,0,15q�$��Center=�$��5,0,0,0q� �� 
IsReadOnly . �� $��Кем подписан:��$
��Bold"�$��0,0,0,5q��� -   $ SignedByTextBox��$��30��$��0,0,0,15q�$��Center=�$��5,0,0,0q� . �� $��Подпись:��$
��Bold"�$��0,0,0,5q��� -   $	 SignatureСomboBox��$��30��$��0,0,0,15q�$��7,0,0,0q�$��Center=��� $&�� Дата регистрации:��$
��Bold"�$��0,0,0,5q�(  "System.Windows.Controls.DatePicker  -     Name$ RegistrationDatePicker��$��30��
�������$��5,0,0,0q�$��Center=��� -   $��EditBtn��$��Изменить��#��[ $��Bottom=�$��Center=�$��
275,17,0,5q�$��80��$��20��$��1��$��12(�
���   �