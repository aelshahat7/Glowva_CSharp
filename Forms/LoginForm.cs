using GlowvaERP.Services;

namespace GlowvaERP.Forms;

public sealed class LoginForm:Form
{
 readonly TextBox user=new(),pass=new(); public LoginForm(){Text="تسجيل الدخول - Glowva ERP";ClientSize=new Size(500,320);StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;RightToLeft=RightToLeft.Yes;var t=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=5,Padding=new Padding(35),RightToLeft=RightToLeft.Yes};t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,30));t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,70));user.Dock=pass.Dock=DockStyle.Fill;pass.UseSystemPasswordChar=true;user.Text="admin";var login=new Button{Text="دخول",Dock=DockStyle.Fill,BackColor=Color.FromArgb(33,150,243),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};login.Click+=(_,_)=>DoLogin();t.Controls.Add(L("المستخدم"),0,0);t.Controls.Add(user,1,0);t.Controls.Add(L("كلمة المرور"),0,1);t.Controls.Add(pass,1,1);t.Controls.Add(login,1,3);var hint=L("المستخدم الافتراضي: admin / admin");hint.ForeColor=Color.DimGray;t.Controls.Add(hint,0,4);t.SetColumnSpan(hint,2);Controls.Add(t);AcceptButton=login;}
 void DoLogin(){if(AuthService.Login(user.Text,pass.Text)){DialogResult=DialogResult.OK;Close();}else{MessageBox.Show(this,"اسم المستخدم أو كلمة المرور غير صحيحة.","تسجيل الدخول",MessageBoxButtons.OK,MessageBoxIcon.Warning);pass.SelectAll();pass.Focus();}}
 static Label L(string x)=>new(){Text=x,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleRight,Font=new Font("Segoe UI",10,FontStyle.Bold)};
}