   M S B A M L   `   `   `  ����E  AArchiveApp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'clr-namespace:ArchiveApp
ArchiveApp  5   	       ArchiveApp.MainWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   *localclr-namespace:ArchiveApp     5   	     � Icon$   /Resources/ArchiveIcon.png��5   	    � Title$ Archive��6+    � 
ResizeMode$ CanResizeWithGrip=�5	   	    � WindowStyle$ SingleBorderWindow=�6(    � ShowInTaskbar . 
�������$	��500��5   	   $
��1200��6   $	��600��6)   $
��1400��69    � 	Resources 5         RotateAnimation �� TargetProperty W� From	 W� To	 g� Duration  ReloadAnimation
 g� 	BeginTime��%�  &	        &	 �     �� 5   
   .��5      W� $9 3(UIElement.RenderTransform).(RotateTransform.Angle)�5      $ 0X�5      $	 360X�6   $	 0:0:0.5D�6#   5      �� 5   
   .��5      W� $ Opacity�5      $ 1X�5      $ 0X�6   $	 0:0:0.3D�6!   $
 0:0:0.1X�5      W� 5      $ Opacity�5      $ 0X�5      $ 1X�6   $	 0:0:0.3D�6!   $
 0:0:0.2X�5      5      5      .��5      � -   5         �Name$ MainGrid��5       � ColumnDefinitions 5   
   �� 5      $	��160 �6   �� 5        � RowDefinitions 5"   
   �� 5#      $��40 �6   �� 5$      $��* �6   �� 5%      $��0* �6   .+�5'   
   � ���#bae3e8$��0��6$   $��2��61   6G   � 5(   
   -   5        $��MenuGrid��5(      ���#bae3e8$��Stretch=�64   $��Stretch=�6P   $��1��5)       K� ZIndex$ -1��6   |�.   � FocusVisualStyle 6@   Z� 6^   �� 5*   
   -   5        $��TitleTxt��5*      $��16(�6$   $��АИС "ИС Архив"��62   $
��Left=�6H   $��Center=�5+      $��30,0,0,0q�5,      $��1��6&   68   �� 5-   
   -   5        $��RepBtn��5-      $��1��6   $��25��6/   $��25��6:   $��Center=�6F   $��Right=�5.      $��0q�6-   $��0,0,60,0q�6A   $ 1��6S   
������ .��50      �� $��/Resources/RepIcon.png��6   66   51      �� 52   
   -   5        $��MainBtn��52      
������ $��1��69   $��50��6F   $	��Top=�6R   $��Stretch=�53      $��0q�54      $��Главное��6%   $
��Left=�55      $��20,0,0,0q�63   $��14(�6F   6T   �� 56   
   -   5        $��DocBtn��56      
������ $��1��68   $��50��6E   $	��Top=�6Q   $��0,50,0,0q�57      $��Stretch=�6#   $��0q�6A   $��Документы��58      $
��Left=�6%   $��20,0,0,0q�6G   $��14(�6Z   6h   �� 59   
   -   5        $��ReqBtn��59      
������ $��1��68   $��50��6E   $	��Top=�6Q   $��	0,150,0,0q�5:      $��Stretch=�6$   $��0q�6C   $��Запросы��5;      $
��Left=�6#   $��20,0,0,0q�6E   $��14(�6X   6f   �� 5<   
   -   5        $��CardBtn��5<      
������ $��1��69   $��50��6F   $	��Top=�6R   $��	0,100,0,0q�5=      $��Stretch=�6$   $��0q�6B   $��Карточки��5>      $
��Left=�6$   $��20,0,0,0q�6F   $��14(�6Y   6g   �� 5?   
   -	   5        $��ExitBtn��5?      
������ $��1��69   $��50��6F   $��Bottom=�6R   $��0,0,0,0q�5@      $��Stretch=�6"   $��0q�6@   $��
Выйти��5A      $
��Left=�6!   $��20,0,0,0q�6C   $��14(�6V   6d    � 5B   
   -
   5        $��	MainFrame��5B   M     � NavigationUIVisibility$ Hidden=�6   $��1��60   $��1��6=   6^   �� 5C   
   -   5        $��	SearchBtn��5C      $��1��6"   $��25��62   $��25��6=   $��Center=�6I   $��Right=�5D      $��	0,0,140,0q�6-   $ 1��6@   
�������.��5F      �� $��/Resources/SearchIcon.png��6   69   5G      �� 5H   
   -   5        $��NotBtn��5H      
������ $��0q�68   $��1��6L   $��Center=�5I      $��Right=�6,   $��25��6H   $��25��5J      $��	0,0,100,0q�6   .��5K      �� $��/Resources/NotIcon.png��6   66   5L      �� 5M   
   -   5        $��
RefreshBtn��5M      
������ $��0q�6<   $��1��6P   $��Center=�5N      $��Right=�6,   $��25��6H   $��25��5O      $��0,0,20,0q�6   z�5P      �� 5Q       �� CenterX$
 12.5Q�6"    �� CenterY$
 12.5Q�61   .��5S      �� $��/Resources/UpdateIcon.png��6   69   5T      �� 5U   
   $ ��/Resources/ArchiveIcon.png��6   $��Center=�64   $
��Left=�6O   $��30��6j   $��30��5V      $��15,0,0,0q�6   6/   �� 5W   
   -   5        $��	BurgerBtn��5W      
������ $��0q�6;   $��Center=�6O   $��Right=�5X      $��30��5Y      $��30��6   $��0,0,30,0q�6(   .��5Z      �� $��/Resources/BurgerIcon.png��6   69   5[      �� 5\   
   -   5        $��	SearchBox��5\      $��1��6#   $��25��63   $	��250��6?   $��Center=�6K   $��Center=�5]      $��Right=�5^      $
��Left=�6.   $��	0,0,140,0q�6P   $��5,0,0,0q�5_      5`   9   �� 5a   
   -   5        $��
SearchText��5a      $��
Поиск��6&   $��1��63   $��Center=�6C   $��Right=�5b       �� TextAlignment$
 Left=�60   $��14(�6E   $��	0,0,133,0q�5c       J� IsHitTestVisible . $	��255��6?   $��30��6K    �� Padding$ 	5,5.5,0,0q�6W   6�   5d      5e      