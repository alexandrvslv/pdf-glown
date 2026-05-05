/*
 * https://github.com/apache/pdfbox
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
namespace PdfGlown.Documents.Contents.Fonts.CCF
{
	/**
	 * This is specialized CFFEncoding. It's used if the EncodingId of a font is set to 0.
	 * 
	 * @author Villu Ruusmann
	 */
	public sealed class CFFStandardEncoding : CFFEncoding
	{
		private CFFStandardEncoding()
		{
            Add(0, 0);
            Add(1, 0);
            Add(2, 0);
            Add(3, 0);
            Add(4, 0);
            Add(5, 0);
            Add(6, 0);
            Add(7, 0);
            Add(8, 0);
            Add(9, 0);
            Add(10, 0);
            Add(11, 0);
            Add(12, 0);
            Add(13, 0);
            Add(14, 0);
            Add(15, 0);
            Add(16, 0);
            Add(17, 0);
            Add(18, 0);
            Add(19, 0);
            Add(20, 0);
            Add(21, 0);
            Add(22, 0);
            Add(23, 0);
            Add(24, 0);
            Add(25, 0);
            Add(26, 0);
            Add(27, 0);
            Add(28, 0);
            Add(29, 0);
            Add(30, 0);
            Add(31, 0);
            Add(32, 1);
            Add(33, 2);
            Add(34, 3);
            Add(35, 4);
            Add(36, 5);
            Add(37, 6);
            Add(38, 7);
            Add(39, 8);
            Add(40, 9);
            Add(41, 10);
            Add(42, 11);
            Add(43, 12);
            Add(44, 13);
            Add(45, 14);
            Add(46, 15);
            Add(47, 16);
            Add(48, 17);
            Add(49, 18);
            Add(50, 19);
            Add(51, 20);
            Add(52, 21);
            Add(53, 22);
            Add(54, 23);
            Add(55, 24);
            Add(56, 25);
            Add(57, 26);
            Add(58, 27);
            Add(59, 28);
            Add(60, 29);
            Add(61, 30);
            Add(62, 31);
            Add(63, 32);
            Add(64, 33);
            Add(65, 34);
            Add(66, 35);
            Add(67, 36);
            Add(68, 37);
            Add(69, 38);
            Add(70, 39);
            Add(71, 40);
            Add(72, 41);
            Add(73, 42);
            Add(74, 43);
            Add(75, 44);
            Add(76, 45);
            Add(77, 46);
            Add(78, 47);
            Add(79, 48);
            Add(80, 49);
            Add(81, 50);
            Add(82, 51);
            Add(83, 52);
            Add(84, 53);
            Add(85, 54);
            Add(86, 55);
            Add(87, 56);
            Add(88, 57);
            Add(89, 58);
            Add(90, 59);
            Add(91, 60);
            Add(92, 61);
            Add(93, 62);
            Add(94, 63);
            Add(95, 64);
            Add(96, 65);
            Add(97, 66);
            Add(98, 67);
            Add(99, 68);
            Add(100, 69);
            Add(101, 70);
            Add(102, 71);
            Add(103, 72);
            Add(104, 73);
            Add(105, 74);
            Add(106, 75);
            Add(107, 76);
            Add(108, 77);
            Add(109, 78);
            Add(110, 79);
            Add(111, 80);
            Add(112, 81);
            Add(113, 82);
            Add(114, 83);
            Add(115, 84);
            Add(116, 85);
            Add(117, 86);
            Add(118, 87);
            Add(119, 88);
            Add(120, 89);
            Add(121, 90);
            Add(122, 91);
            Add(123, 92);
            Add(124, 93);
            Add(125, 94);
            Add(126, 95);
            Add(127, 0);
            Add(128, 0);
            Add(129, 0);
            Add(130, 0);
            Add(131, 0);
            Add(132, 0);
            Add(133, 0);
            Add(134, 0);
            Add(135, 0);
            Add(136, 0);
            Add(137, 0);
            Add(138, 0);
            Add(139, 0);
            Add(140, 0);
            Add(141, 0);
            Add(142, 0);
            Add(143, 0);
            Add(144, 0);
            Add(145, 0);
            Add(146, 0);
            Add(147, 0);
            Add(148, 0);
            Add(149, 0);
            Add(150, 0);
            Add(151, 0);
            Add(152, 0);
            Add(153, 0);
            Add(154, 0);
            Add(155, 0);
            Add(156, 0);
            Add(157, 0);
            Add(158, 0);
            Add(159, 0);
            Add(160, 0);
            Add(161, 96);
            Add(162, 97);
            Add(163, 98);
            Add(164, 99);
            Add(165, 100);
            Add(166, 101);
            Add(167, 102);
            Add(168, 103);
            Add(169, 104);
            Add(170, 105);
            Add(171, 106);
            Add(172, 107);
            Add(173, 108);
            Add(174, 109);
            Add(175, 110);
            Add(176, 0);
            Add(177, 111);
            Add(178, 112);
            Add(179, 113);
            Add(180, 114);
            Add(181, 0);
            Add(182, 115);
            Add(183, 116);
            Add(184, 117);
            Add(185, 118);
            Add(186, 119);
            Add(187, 120);
            Add(188, 121);
            Add(189, 122);
            Add(190, 0);
            Add(191, 123);
            Add(192, 0);
            Add(193, 124);
            Add(194, 125);
            Add(195, 126);
            Add(196, 127);
            Add(197, 128);
            Add(198, 129);
            Add(199, 130);
            Add(200, 131);
            Add(201, 0);
            Add(202, 132);
            Add(203, 133);
            Add(204, 0);
            Add(205, 134);
            Add(206, 135);
            Add(207, 136);
            Add(208, 137);
            Add(209, 0);
            Add(210, 0);
            Add(211, 0);
            Add(212, 0);
            Add(213, 0);
            Add(214, 0);
            Add(215, 0);
            Add(216, 0);
            Add(217, 0);
            Add(218, 0);
            Add(219, 0);
            Add(220, 0);
            Add(221, 0);
            Add(222, 0);
            Add(223, 0);
            Add(224, 0);
            Add(225, 138);
            Add(226, 0);
            Add(227, 139);
            Add(228, 0);
            Add(229, 0);
            Add(230, 0);
            Add(231, 0);
            Add(232, 140);
            Add(233, 141);
            Add(234, 142);
            Add(235, 143);
            Add(236, 0);
            Add(237, 0);
            Add(238, 0);
            Add(239, 0);
            Add(240, 0);
            Add(241, 144);
            Add(242, 0);
            Add(243, 0);
            Add(244, 0);
            Add(245, 145);
            Add(246, 0);
            Add(247, 0);
            Add(248, 146);
            Add(249, 147);
            Add(250, 148);
            Add(251, 149);
            Add(252, 0);
            Add(253, 0);
            Add(254, 0);
            Add(255, 0);
        }

		public static readonly CFFStandardEncoding Instance = new CFFStandardEncoding();

		static CFFStandardEncoding()
		{
			
		}
	}
}