import unittest,tempfile,importlib.util
from pathlib import Path
spec=importlib.util.spec_from_file_location('styleguard',Path(__file__).parents[2]/'tools/Check-NoStylingOverrides.py');guard=importlib.util.module_from_spec(spec);spec.loader.exec_module(guard)
class StylingBoundary(unittest.TestCase):
 def fixture(self,text):
  temp=tempfile.TemporaryDirectory();self.addCleanup(temp.cleanup);root=Path(temp.name);(root/'game').mkdir();(root/'game/Test.cs').write_text(text,encoding='utf-8');return root
 def test_override_is_rejected(self):self.assertTrue(guard.scan(self.fixture('button.AddThemeColorOverride("font_color", value);')))
 def test_new_stylebox_is_rejected(self):self.assertTrue(guard.scan(self.fixture('var plate = new StyleBoxFlat();')))
 def test_literal_minimum_is_rejected(self):self.assertTrue(guard.scan(self.fixture('CustomMinimumSize = new Vector2(240, 40);')))
 def test_styling_font_and_color_are_rejected(self):self.assertTrue(guard.scan(self.fixture('var font = new SystemFont(); var color = new Color(1,0,0);')))
 def test_theme_variation_and_theme_dimensions_are_allowed(self):self.assertEqual([],guard.scan(self.fixture('ThemeTypeVariation = "CommandKey"; CustomMinimumSize = new Vector2(GetThemeConstant("key_width"), GetThemeConstant("key_height"));')))
 def test_baseline_allowance_rejects_duplicate(self):
  root=self.fixture('var plate = new StyleBoxFlat();');allowed=guard.occurrences(root);self.assertEqual([],guard.scan(root,allowed));(root/'game/Test.cs').write_text('var plate = new StyleBoxFlat();\nvar plate = new StyleBoxFlat();',encoding='utf-8');self.assertEqual(1,len(guard.scan(root,allowed)))
 def test_theme_numeric_zero_is_layout_neutral(self):self.assertEqual([],guard.scan(self.fixture('CustomMinimumSize = new Vector2(0, GetThemeConstant("key_height"));')))
if __name__=='__main__':unittest.main()
