   M S B A M L   `   `   `  ����   
ArchiveApp"    ArchiveApp.ReportOptionsPage   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     @d2http://schemas.microsoft.com/expression/blend/2008       N� Title$%  Параметры отчета�� N� 
Background
 ����� � 	Resources     BooleanToVisibilityConverter��%   &	        �� .���  � RowDefinitions �� $��* ��� $
��Auto �.+��� $
��Auto=�$��Disabled=�.��� -   $��MainGrid��$��10q� �� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto ��� $��* ��� $
��Auto �.+��� $��Формат отчета��$��0��$��0,0,0,10q�.���� .+�	� -    	�Name$ 	WordRadio��$
��Word�� d� 	IsChecked$
 TrueY�$��5q�	� -   $ 
ExcelRadio��$��Excel��$��5q�	� -   $ PdfRadio��$	��PDF��$��5q��� $-��'Формат представления��$��1��$��0,0,0,10q�y��� *IsWordOrPdf+ �� 	Converter# [  .���� .+�	� -   $ TableFormatRadio��$��Таблица��$
 TrueY�$��5q�	� -   $ TextFormatRadio��$��
Текст��$��5q��� $.��(Таблицы для включения��$��2��$��0,0,0,10q�.���� .+��� -    ��Name$ AllTablesCheckBox��$��Все таблицы��$��5q��� -   $ DocumentsCheckBox��$��Документы��$��5q��� -	    ��Name$ DocumentsComboBox��$��15,5,5,5q�$y�	Collapsed=��� -
   $ RequestsCheckBox��$��Запросы��$��5q��� -   $ RequestsComboBox��$��15,5,5,5q�$y�	Collapsed=��� -   $ UsersCheckBox��$��Пользователи��$��5q��� -   $ UsersComboBox��$��15,5,5,5q�$y�	Collapsed=��� -   $ RegCardsCheckBox��$/��)Регистрационные карты��$��5q��� -   $ RegCardsComboBox��$��15,5,5,5q�$y�	Collapsed=��� $��Период отчета��$��3��$��0,0,0,10q�y��� *IsFullReport+# [  .���� .+�(  "System.Windows.Controls.DatePicker  -   	  Name$	 StartDatePicker��$��5q�
   SelectedDate
 �� *	StartDate+  -   $	 EndDatePicker��$��5q�
 �� *	EndDate+�� $��5�� �� Orientation$ 
Horizontal=�$��Right=�$��Bottom=�.+��� -   $)��#Сформировать отчет��$	��150��$��5q�$��	0,0,30,12q����#bae3e8