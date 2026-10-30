// Night1ScareEvent.cs  (v1.3.16)
// جدید در v1.3.16 — به درخواست کاربر: «نمیخوام اصلا دوربین خودش بره روی هیولا، اینو بردار کلا —
//   یه کاری کن تا زمانی دوربین نیفتاده روی هیولا، شروع به حرکت کردن نکنه»:
//   • تعقیب خودکار دوربین (v1.3.8) کلاً حذف شد — دوربین CAM هیچ‌وقت خودش نمی‌چرخه؛
//     فقط پلیر می‌تونه دستی بچرخونش (دقیقاً مثل قبل از v1.3.8)
//   • شرط شروع جدید — Start Only When Man In Frame (پیش‌فرض روشن): مرد تا وقتی
//     پلیر روی CAM رویداد نیست «و» خودِ دوربین واقعاً به سمتش نیست (هیولا توی کادر
//     دیده بشه) اصلاً ظاهر نمیشه و راه نمی‌افته. پلیر باید دوربین رو دستی بچرخونه تا
//     هیولا رو پیدا کنه → همون لحظه رویداد شروع میشه و قفل میشه
//   • تا وقتی شرط برقرار نیست، هر چند ثانیه (Waiting In Frame Log Interval) لاگ یادآوری میده
//   • SaveCctvCameraRotation/RestoreCctvCameraRotation هم حذف شدن — دیگه هیچ چرخشی
//     توسط کد انجام نمیشه که نیاز به برگردوندن داشته باشه
// جدید در v1.3.15 — «برگشت بیضی سبز بزرگ» بعد از v1.3.13/v1.3.14 حل شد:
//   • محافظ v1.3.13 همه‌چیزِ زیر VirtualCamera/LiveCamera رو دست‌نخوردنی کرده بود —
//     از جمله هاله‌ی گلوِ خود Spot Light! چون v1.3.14 نور دوربین رو روشن میکنه و
//     دوربین وسط رویداد به هیولا خیره‌ست، همین هاله میفته وسط کادر = بیضی سبز بزرگ.
//   • محافظت حالا «هوشمند» شد: فقط افکت دید در شب (Camera Volume / هر رندری زیر Volume
//     یا با اسم overlay/noise/static/...) محافظت میشه؛ هاله‌ی گلوِ خود دوربین اجازه داره
//     پنهان شه — همون کاری که v1.3.9 میکرد و بیضی رو حل کرده بود.
//   • دو فیلد دستی جدید: Cctv Glare Force Hide (همیشه پنهان کن) / Cctv Glare Never Hide
//     (هیچ‌وقت پنهان نکن) — اگه تشخیص خودکار اشتباه کرد، آبجکت رو بکش توی لیست درست.
//   • لاگ 🛡️: هر رندرِ بزرگِ محافظت‌شده‌ی CAM یک بار در هر رویداد لاگ میشه — اگه بیضی
//     موند، همین لاگ دقیق میگه کدوم آبجکته.
//   • منوی راست‌کلیک جدید: «Debug: CAM Renderers (لیست رندرهای زیر دوربین رویداد)»
// جدید در v1.3.14 — ریشه‌ی واقعی «نه چراغ هست نه افکت دید در شب روی CAM 5» پیدا شد:
//   • کاربر ساختار دقیق ریک دوربین رو فرستاد (برای همه‌ی دوربین‌ها یکسانه):
//       SecurityCamera (4) → Bracket → HorizontalJoint → SecurityCamera → Camera
//           ├─ LiveCamera                  ← فید مانیتور CRT
//           └─ Virtual Camera              ← نمای تمام‌صفحه
//               ├─ Camera Volume           ← افکت دید در شب
//               └─ Spot Light              ← نور دید در شب
//   • یعنی Spot Light و Camera Volume هر دو «فرزندِ Virtual Camera» هستن!
//     پس فقط چرخوندن VirtualCamera (v1.3.8) کافیه — نور و افکت خودکار همراهش می‌چرخن.
//     کدِ جدا برای چرخوندن نور (v1.3.11 پین + v1.3.12 نشانه‌گیری هندسی) نه‌تنها بی‌کاربرد بود،
//     بلکه هر فریم چرخش جهانیِ نور رو بازنویسی می‌کرد و با چرخش والدش می‌جنگید —
//     نتیجه: نور به جهت اشتباه می‌رفت (حتی کامل به عقب) → «نه چراغ، نه افکت».
//   • v1.3.14: تمام کدهای چرخاندن نور حذف شد — نور ۱۰۰٪ تابع والدشه، دقیقاً مثل وقتی که
//     کاربر خودش دوربین رو دستی می‌چرخونه (همونی که کاربر گفت درست کار می‌کنه!)
//   • محافظ جدید — Ensure Cctv Light On: موقع شروع رویداد، اگه نورِ زیر VirtualCamera/
//     LiveCamera دوربینِ رویداد خاموش باشه، خودکار روشن میشه + وضعیت کاملش لاگ میشه
//   • ابزار جدید: راست‌کلیک روی کامپوننت → «Debug: CAM Rig State (وضعیت کامل ریک دوربین رویداد)»
//     → یک‌جا لاگ میکنه: VirtualCamera فعال/غیرفعال؟ LiveCamera و تکسچرش؟ Camera Volume
//     (افکت دید در شب) فعال/وزن/پروفایل؟ Spot Light شدت/زاویه/برد؟ CCTVEffects کیت؟
//     ⚠️ اگه بازم نور/افکت نبود، خروجی همین منو رو بفرست — دقیق میگه کدوم کامپوننته
// جدید در v1.3.13 — رفع «با انتخاب CAM 5 نه چراغ هست نه افکت دید در شب»:
//   • علت واقعی پیدا شد: سیستم «مخفی‌کننده‌ی هاله» (v1.3.9) که برای بیضی سبز ساخته بودیم،
//     هر چیز درخشانی که ≥۱۵٪ صفحه بود پنهان می‌کرد — از جمله افکت سبز دید در شب و
//     هاله‌ی اسپات‌لایت که زیرِ خودِ دوربین CAM هستن! (احتمالاً همون «بیضی سبز» اول هم
//     افکت دید در شب خودت بود!)
//   • حالا تجهیزات خودِ دوربین‌های CAM (زیرمجموعه‌ی VirtualCamera/LiveCamera همه دوربین‌ها
//     + نورهای قفل‌شده‌ی تعقیب) هیچ‌وقت «هاله» حساب نمیشن — نه پنهان میشن نه فلرشون خاموش میشه
//   • نجات فوری: اگه قبلاً اشتباهی پنهان شده بودن، همون اول اسکن برمی‌گردن
// جدید در v1.3.12 — نسخه‌ی قطعی «نور دوربین موقع تعقیب نمی‌چرخه»:
//   • v1.3.11 نور رو به ترنسفورم دوربین «پین» کرده بود؛ ولی اگه سیمی‌کازی داخل کیت فرق کنه
//     (مثلاً VirtualCamera معرفی‌شده والدِ نور نباشه، یا کیت/اسکریپت دیگه‌ای زاویه‌ها رو دستکاری کنه)
//     نور باز هم سر جاش می‌مونه — و چون کاربر خودش والدِ دوربین رو دستی می‌چرخونه و همه‌چیز
//     درست کار می‌کنه، مشکل دقیقاً از همین وابستگی به ترنسفورم بود
//   • v1.3.12: نور دیگه به هیچ ترنسفورمی وابسته نیست — «نشانه‌گیری هندسی»:
//     هر فریم نور با همون نرمی دوربین مستقیم به سمت خود هیولا می‌چرخه (مستقل از والد،
//     مستقل از کیت، مستقل از هر چیز دیگه!) — و آخر رویداد به زاویه اول برمی‌گرده
//   • Cctv Light Center On Man: اگه تیک بخوره نور دقیقاً وسط هیولا قفل میشه؛
//     بی‌تیک = زاویه‌ی اولیه‌ی نور نسبت به هیولا حفظ میشه (رفتار طبیعی‌تر)
//   • نور در فاز انتظار هم روی هیولا میمونه (خودترمیم — اگه کیت زاویه‌ها رو ریست کنه، نور برمی‌گرده)
//   • لاگ عیب‌یابی Cctv Light Debug Log (پیش‌فرض روشن): هر ۲ ثانیه زاویه‌ی نور↔دوربین
//   • راست‌کلیک روی کامپوننت → «Debug: CCTV Light Snapshot» → وضعیت کامل نور/دوربین‌ها
// جدید در v1.3.11 — رفع «اسپات‌لایت دوربین موقع تعقیب هیولا نمی‌چرخه»:
//   • علت: افکت دید در شب (Volume) فرزندِ VirtualCamera هست و باهاش می‌چرخه، ولی
//     Spot Light معمولاً زیرِ بدنه/سرِ مدل دوربینه — نه زیر VirtualCamera نه زیر LiveCamera؛
//     چون v1.3.8 فقط همون دو تا ترنسفورم رو می‌چرخوند، نور سر جاش می‌موند و مخروط نور
//     یه جهت دیگه رو روشن می‌کرد در حالی که تصویر چرخیده بود
//   • حالا موقع تعقیب، نورهای نزدیک دوربین CAM (خودش + فرزندان + تا ۲ سطح والد) دقیقاً
//     هم‌چرخش با دوربین می‌چرخن (با حفظ زاویه اولیه نسبت به دوربین) — آخر رویداد یا
//     اول شب بعد همه به زاویه اول برمی‌گردن (هیچی از صحنه حذف نمیشه)
//   • فیلتر شعاع Cctv Light Search Radius (پیش‌فرض ۴ متر): نور بقیه‌ی دوربین‌ها یا
//     چراغ‌های بی‌ربط صحنه دست نمی‌خورن؛ نور مرد و پلیر هم مستثنی هستن
//   • اگه خواستی دستی نور مشخص کنی: فیلد Cctv Extra Lights توی Inspector
// جدید در v1.3.10 — رفع «مرد موقع جامپ‌اسکر از پشت مانیتور دیده نمیشه»:
//   • لاگ کاربر ثابت کرد: وقتی جامپ‌اسکر از داخل نمای CCTV شروع میشه، دوربین اصلی هنوز
//     روی پوزِ CAM (برج) هست → مرد پای برج و ۹.۶ متر پایین‌تر از دوربین اسپاون میشد
//     (دوربین Y=59.77، کف مرد Y=50.18!) و پلیر هیچیش نمیدید
//   • حالا اگه جامپ‌اسکر از نمای CCTV شروع شه، اسپاون از «ریگ خود پلیر» محاسبه میشه
//     (همون‌جا که نشستی سر مانیتور) → مرد همیشه درست جلوی خودت ظاهر میشه
// جدید در v1.3.9 — رفع «بیضی سبز بزرگ روی CAM 5»:
//   • وقتی دوربین برای تعقیب هیولا می‌چرخه (v1.3.8)، بعضی «هاله‌های نورانی» صحنه (گلو چراغ‌ها،
//     فلر لنز، پارتیکل‌ها) وارد کادر میشن و چون دوربین ثابت بهشون خیره‌ست، نصف صفحه رو میگیرن
//   • Auto Hide Cctv Glare ✅: وسط رویداد، هر هاله/فلر بزرگی که توی کادر CAM پیداش بشه
//     موقتاً پنهان میشه — آخر رویداد یا وقتی از نمای دوربین خارج شدی، همه خودکار برمی‌گردن
//     (هیچی از صحنه حذف یا خراب نمیشه)
//   • اگه بازم بیضی سبز دیدی: راست‌کلیک روی کامپوننت → «Debug: Log Green Ellipse Culprit»
//     در حالی که بیضی روی صفحه‌ست → دقیق لاگ میکنه بزرگ‌ترین چیزهای روی صفحه کدوما هستن
// جدید در v1.3.8 — دوربین CAM هیولا رو دنبال می‌کنه (طرح درست کاربر):
//   • کاربر بین دوربین‌ها سوییچ می‌کنه → میرسه به CAM 5 → قفل میشه → هیولا راه میفته
//   • از لحظه شروع راه‌رفتن تا لحظه باز شدن قفل، خودِ دوربین CAM مثل دوربین مداربسته واقعی
//     هیولا رو تعقیب می‌کنه (نمای تمام‌صفحه = VirtualCamera، فید مانیتور = LiveCamera —
//     هر دو نرم به سمت مرد می‌چرخنن با سرعت Cctv Track Speed)
//   • بعد از باز شدن قفل، دوربین همون‌جا که هیولاست می‌مونه (کاربر میره برق‌ها رو خاموش کنه)
//   • اول شب بعدی، زاویه دوربین خودکار به حالت اول برمی‌گرده
// جدید در v1.3.7 — رفع «بعد از باز شدن قفل هیچی نمیشه»:
//   • لاگ‌های کاربر نشون داد بعد از «قفل دوربین باز شد» دیگه هیچ لاگی نمیاد — یعنی جامپ‌اسکر
//     اصلاً اجرا نمیشه (نه اینکه اجرا شه و نچرخه)! رویداد توی فاز انتظار بی‌صدا می‌خوابه
//   • علت اصلی: تشخیص زل‌زدن فقط وقتی کار می‌کرد که پلیر بیرون از نمای دوربین باشه —
//     در نتیجه اگه بعد از باز شدن قفل ادامه می‌دادی CAM 5 رو نگاه می‌کردی (طبیعی‌ترین کار!
//     مرد که اومده جلوی دوربین و داره بهت زل میزنه)، هیچ‌وقت هیچی نمیشد!
//   • v1.3.7: حالا زل زدن از پشت مانیتور هم کار میکنه (Stare Works In Cctv View = ✅) —
//     مرد که جلوی CAM 5 وایساده و داری نگاهش می‌کنی، بعد از Stare Time Limit می‌پره سمتت!
//   • v1.3.7: لاگ وضعیت فاز انتظار («تایمر: فعال/غیرفعال»، «زل‌زدن: فعال/غیرفعال») —
//     اگه بازم چیزی نشد، از همین لاگ دقیق معلوم میشه کدوم مکانیزم توی Inspector غیرفعاله
//   • v1.3.7: پیش‌شماره تایمر — ۵ ثانیه مونده به جامپ‌اسکر لاگ میده («چراغ‌ها رو خاموش کن!»)
// جدید در v1.3.6 — ریشه‌ی اصلی «نچرخیدن» پیدا شد: CinemachineBrain!
//   • روی دوربین اصلی، CinemachineBrain هر فریم (LateUpdate) خروجی vcam رو روی دوربین مینویسه
//     و چرخش دستی ما رو پاک میکنه — برای همین نه چرخش مستقیم دوربین در v1.3.4 اثر داشت
//     نه چرخوندن ریگ نگاه در v1.3.5
//   • حالا موقع Force Look، خودِ CinemachineBrain موقتاً خاموش میشه (با اسم کامپوننت —
//     بدون وابستگی کامپایل، هم CM 2.x هم 3.x)، دوربین به صورت «مطلق» روی صورت مرد قفل میشه
//     و آخر جامپ‌اسکر Brain دوباره روشن میشه
//   • لاگ جدید: اگه Brain پیدا نشد، Console دقیق میگه («CinemachineBrain پیدا نشد»)
// جدید در v1.3.5 — Force Look واقعی (چرا v1.3.4 نچرخید؟):
//   • دوربین اصلی UHFPS («MainCameraBrain») رو Cinemachine Brain کنترل میکنه —
//     چرخش مستقیم Transform دوربین هر فریم توسط Brain بازنویسی میشه و اثری نداشت
//   • حالا دو مسیر موازی (هر دو به صورت مرد نشانه میرن):
//     1) LookController خود کیت (با رفلکس — بدون وابستگی کامپایل) — LerpRotation(face, 0.2s, locked)
//        و هر 0.75s دوباره اعمال میشه تا اگه فیدِ برگشت CCTV افکتش رو خورد، ترمیم شه
//     2) چرخوندن «ریگ نگاه» (جدِ دوربین تا ریشه‌ی پلیر) — vcam دنبال ریگ میره و Brain تبعیت میکنه
//   • اگه بازم نچرخید، لاگ اسم ریگ رو نشون میده («Force Look — ریگ: …») — بفرستش
// جدید در v1.3.4 — Force Look (قفل اجباری دوربین روی صورت مرد):
//   • مستقل از افکت‌های کیت، خود اسکریپت موقع جامپ‌اسکر دوربین رو سریع می‌چرخونه
//     سمت صورت مرد و تا آخر جامپ‌اسکر قفل نگه میداره → کاربر حتماً هیولا رو میبینه
//   • اگه سر مرد با انیمیشن جیغ تکون بخوره، دوربین دنبالش میمونه
//   • Force Look At Man = تیک/بی‌تیک | Look Snap Speed = سرعت چرخش (بالا = یهویی)
// جدید در v1.3.3 — دو رفع دیگه برای «مرد دیده نمیشه»:
//   • کف واقعی زیر نقطه اسپاون با Raycast پیدا میشه (Ground Mask) — مرد دیگه با
//     ارتفاع کفِ اتاقِ دوربینِ CAP 5 اسپاون نمیشه (توی برج چندطبقه مهمه!)
//   • اگه جامپ‌اسکر از داخل نمای CCTV شروع شده باشه، چون برگشت به دوربین پلیر
//     با فید زمان میبره، Cctv Return Buffer ثانیه به زمان نمایش اضافه میشه
//     وگرنه فیدِ ریست قبل از اینکه مرد رو ببینی میاد!
// جدید در v1.3.2 — رفع «مرد جلوی صورت پلیر نمیاد»:
//   • دوربین پلیر اول شروع صحنه کش میشه (Camera.main وسط خروج از نمای CCTV ممکنه null بده
//     چون SwitchBack اول VirtualCamera رو خاموش میکنه و برگشت به دوربین پلیر با فیدِ کند انجام میشه)
//   • پیدا کردن دوربین پلیر سه مرحله‌ای: Camera.main → دوربین زیرمجموعه‌ی CharacterController → تگ MainCamera
//   • تلپورت مرد فقط با جهت افقی نگاه محاسبه میشه (اگه پلیر به پایین نگاه کنه،
//     مرد دیگه زیر زمین یا داخل پلیر اسپاون نمیشه)
//   • لاگ‌های تشخیصی: دلیل هر رد شدن یا موفقیتِ تلپورت توی Console نوشته میشه —
//     اگه بازم مرد نیومد، Console رو بفرست تا دقیق مشخص شه کجای کاره
// جدید در v1.3.1 — رفع خطای UnassignedReferenceException: LookAtTarget:
//   اگه فیلد Look At Target کامپوننت JumpscareTrigger کیت خالی باشه، موقع جامپ‌اسکر
//   کیت میخواد دوربین پلیر رو به سمت اون تارگت بچرخونه و چون خالیه کرش میکنه.
//   • v1.3.1: اگه Look At Target خالی باشه، خود اسکریپت با استخوان سرِ مرد پرش میکنه
//     (Head Bone Name یا استخوان Head ریگ Humanoid یا خود مدل — هر کدوم پیدا شه)
//   • v1.3.1: افکت‌های کیت داخل try/catch رفتن → اگه کیت به هر دلیلی خطا بده
//     (فیلد خالی، صدای خالی و...)، جامپ‌اسکر خودمون + ریست شب ادامه پیدا میکنه
//   • باز هم بهتره توی Inspector مقدار Look At Target رو دستی با استخوان سر مرد پر کنی
// جدید در v1.3 — جامپ‌اسکر خود کیت (JumpscareTrigger):
//   کامپوننت JumpscareTrigger خود کیت UHFPS رو می‌تونی به این اسکریپت وصل کنی:
//   • فیلد Kit Jumpscare ← کامپوننت JumpscareTrigger
//   • موقع جامپ‌اسکر: TriggerJumpscare کیت صدا زده میشه → وینیت/تنتاکل/لرزش دوربین/ترس کیت
//   • قبل از ریست شب هم افکت کیت خودکار بسته میشه (TriggerJumpscareEnded)
//   ⚠️ توی کامپوننت JumpscareTrigger حتماً: Trigger Type = Event | End Jumpscare With Event = ✅
// جدید در v1.2.2 — عیب‌یابی انیمیشن (مرد فقط کشیده میشه و انیمیشن نداره؟):
//   • فیلد Man Animator خالی باشه → خودش روی مدل پیدا می‌کنه
//   • شروع رویداد: اگه Controller / Trigger مشکل داشته باشه، Console دقیق میگه چیه
//   • راست‌کلیک روی کامپوننت → Debug: Print Animator Info  (وضعیت Animator + لیست Trigger ها)
//   • راست‌کلیک روی کامپوننت → Debug: Test Walk Trigger  (تست دستی انیمیشن راه‌رفتن)
// تغییر v1.2.1: genRunning اول شب false در نظر گرفته میشه
//   (چون پلیر خودش جنراتور رو روشن میکنه) — اگه پلیر هنوز جنراتور رو روشن نکرده باشه
//   و رویداد شروع بشه، تاریکی = نجات خودکار (باگ «جامپ‌اسکر غیرمنطقی» نداریم).
// سناریوی ترسناک شب 1 — کامل خودکار:
//   1) پلیر داره دوربین‌ها رو چک میکنه → وقتی به دوربینِ موردنظر (پیش‌فرض CAM 5) میرسه،
//      کنترل دوربین قفل میشه (نه سوییچ، نه خروج).
//   2) مرد ترسناک ظاهر میشه و آروم (قدم قدم) سمت دوربین راه میفته (پیش‌فرض 5 ثانیه).
//   3) بعد از مدت قفل، کنترل آزاد میشه → پلیر می‌تونه بیاد بیرون.
//   4) حالا نجات با «خاموش شدن جنراتور» انجام میشه (v1.2):
//        • حالت PlayerTurnsOff → خود پلیر باید بره جنراتور رو خاموش کنه
//          → همه چراغ‌ها یهو خاموش میشن → مرد غیب میشه (نجات).
//        • حالت AutoBlackout → خود ایونت وسط خیره‌شدنِ مرد به دوربین،
//          جنراتور رو خاموش می‌کنه → تاریکی ناگهانی → مرد غیب میشه.
//      (خاموش کردن دستی چراغ‌ها هم هنوز به عنوان راه جایگزین کار می‌کنه.)
//   5) اگه جنراتور/چراغ‌ها سر وقت خاموش نشن (تایمر) یا مستقیم به مرد زل بزنی (Stare Detection)
//      → جامپ‌اسکر → باخت → برمی‌گرده به شروع همون شب (همه چیز از اول).
//
// نصب:
//   1) مدل مرد ترسناک رو یه جایی «دور» بذار (مثلاً انتهای راهرویی که CAM 5 نشون میده)
//      — همون‌جا نقطه شروع راه‌رفتنشه. Collider لازم نیست، فقط بصریه.
//   2) یه Empty به اسم مثلاً "ScareTarget" بساز و بذارش نزدیک جلوی دوربین 5
//      (جایی که مرد آخر کار باید باشه).
//   3) یه GameObject خالی (مثلاً "Night1Scare") بساز و این اسکریپت رو بنداز:
//      - Scare Man = مدل مرد
//      - Scare Target Point = اون Empty
//      - Target Lights = چراغ‌های همون مکان (خود Light ها، نه سوییچ‌ها)
//   4) بقیه‌ش خودکاره — CCTV و NightManager خودش پیدا میکنه.
//   5) اختیاری: OnJumpscare رو به افکت‌های خودت وصل کن (فلش، لرزش، خون روی صفحه...).
//      خود اسکریپت: مرد رو می‌کشه جلوی صورت پلیر + صدای اسکریم + ریست شب.
//
// نکته: وسط رویداد، دکمه ثبت گزارش قفله (پلیر نمیتونه با گزارش زدن فرار کنه).
//
// انیمیشن مرد (v1.1): مدل Humanoid (مثلاً از Mixamo) + Animator با سه State:
//   Idle (پیش‌فرض) --Walk--> Walk (loop) --LookCamera--> LookCamera (loop)
//   فیلدهای Man Animator / Walk Trigger / Look Trigger رو پر کن.
//   بعد از رسیدن، سر مرد خودکار به لنز دوربین خیره می‌مونه (Head Look).
//
// جنراتور (v1.2): فیلد Generator رو خالی بذار تا خودش تو صحنه پیدا کنه — هیچ سیمی‌کشی نمی‌خواد.
//   PlayerTurnsOff = تسک بقا: پلیر باید خودش بره جنراتور رو خاموش کنه (پیشنهاد شده)
//   AutoBlackout   = سینمایی: خود ایونت بعد از Blackout Delay ثانیه جنراتور رو خاموش می‌کنه
//   بعد از ریست شب (جامپ‌اسکر یا شب بعد)، جنراتور خودکار دوباره روشن میشه.
//
// دیباگ: راست‌کلیک روی کامپوننت → Debug: Start Scare Now / Jumpscare Now / Stop Generator / Reset Event

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UHFPS.Runtime;

public class Night1ScareEvent : MonoBehaviour
{
    private enum Phase { Idle, Walking, WaitingLights, Vanishing, Jumpscaring, Done }

    [Header("Refs (خالی = خودش پیدا میکنه)")]
    [Tooltip("سیستم دوربین‌های مداربسته")]
    public CCTV_CameraSystem cctv;

    [Header("Trigger")]
    [Tooltip("این رویداد فقط در چه شماره شبِی فعال شه؟")]
    public int nightNumber = 1;

    [Tooltip("شماره دوربینی که رویداد روش شروع میشه (5 = CAM 5)")]
    public int scareCameraNumber = 5;

    [Tooltip("در هر شب فقط یک بار؟")]
    public bool oncePerNight = true;

    [Header("The Man (مرد ترسناک)")]
    [Tooltip("مدل مرد — جایی که توی ادیتور میذاریش = نقطه شروع راه‌رفتنش")]
    public GameObject scareMan;

    [Tooltip("نقطه پایان راه‌رفتن — یه Empty نزدیک جلوی دوربین موردنظر")]
    public Transform scareTargetPoint;

    [Tooltip("چند ثانیه طول بکشه نزدیک شه (قدم قدم)")]
    public float walkDuration = 5f;

    [Tooltip("اختیاری: Animator مرد برای انیمیشن راه‌رفتن")]
    public Animator manAnimator;

    [Tooltip("نام Trigger انیمیشن راه‌رفتن (خالی = کاری نکن)")]
    public string walkTrigger = "Walk";

    [Tooltip("Trigger «نگاه به دوربین» — وقتی راه‌رفتن تمام شد یهو فایر میشه (خالی = کاری نکن)")]
    public string lookTrigger = "LookCamera";

    [Tooltip("اختیاری: Trigger جیغ/حمله موقع جامپ‌اسکر (خالی = کاری نکن)")]
    public string screamTrigger = "";

    [Header("Head Look (خیره شدن سر به دوربین)")]
    [Tooltip("بعد از رسیدن، سر مرد زنده به لنز دوربین خیره می‌مونه (حتی اگه دوربین تکون بخوره)")]
    public bool headLookAt = true;

    [Tooltip("نام استخوان سر (مدل Mixamo: mixamorig:Head)")]
    public string headBoneName = "Head";

    [Tooltip("سرعت چرخش سر — عدد بالا = نگاه «یهویی» و ناگهانی")]
    public float headSnapSpeed = 10f;

    [Header("Camera Lock")]
    [Tooltip("چند ثانیه کنترل دوربین قفل بمونه (سوییچ + خروج)")]
    public float lockDuration = 5f;

    [Header("Phase 2: چراغ‌ها")]
    [Tooltip("چراغ‌های اون مکان — برای نجات باید خاموش شن")]
    public Light[] targetLights;

    [Tooltip("true = همه چراغ‌ها باید خاموش شن | false = خاموش کردن یکی کافیه")]
    public bool requireAllLightsOff = true;

    [Tooltip("بعد از خاموش شدن چراغ‌ها، چند ثانیه بمونه بعد غیب شه")]
    public float vanishDelay = 3f;

    [Header("Fail 1: تایمر (جنراتور/چراغ)")]
    [Tooltip("اگه تا این زمان جنراتور/چراغ‌ها خاموش نشدن = جامپ‌اسکر")]
    public bool useLightsTimer = true;
    public float lightsOffTimeLimit = 25f;

    [Header("Fail 2: زل زدن به مرد")]
    [Tooltip("اگه پلیر مستقیم به مرد زل بزنه (وقتی چراغ‌ها روشنه) = جامپ‌اسکر")]
    public bool useStareDetection = true;
    public float stareTimeLimit = 2.5f;
    [Range(0.5f, 1f)] public float stareDotThreshold = 0.95f;
    public float stareMaxDistance = 30f;

    [Tooltip("v1.3.7: زل زدن به مرد از پشت مانیتور (وقتی CAM 5 رو نگاه می‌کنی) هم جامپ‌اسکر بیاره؟ — مرد که اومد جلوی دوربین و بهت زل زده، اگه ادامه بدی نگاهش کنی میاد سراغت!")]
    public bool stareWorksInCctvView = true;

    [Header("Generator (نجات با خاموش کردن جنراتور)")]
    [Tooltip("اتصال رویداد به جنراتور — نجات با خاموش شدن جنراتور ممکن میشه")]
    public bool useGenerator = true;

    [Tooltip("جنراتور — خالی بذار تا خودش تو صحنه پیدا کنه")]
    public PowerGenerator generator;

    public enum GeneratorScareMode { PlayerTurnsOff, AutoBlackout }

    [Tooltip("PlayerTurnsOff = پلیر باید خودش بره جنراتور رو خاموش کنه | AutoBlackout = خود ایونت وسط صحنه جنراتور رو خاموش می‌کنه")]
    public GeneratorScareMode generatorMode = GeneratorScareMode.PlayerTurnsOff;

    [Tooltip("فقط AutoBlackout: چند ثانیه بعد از خیره‌شدن مرد به دوربین، جنراتور خاموش بشه")]
    public float blackoutDelay = 2.5f;

    [Tooltip("بعد از ریست شب، جنراتور دوباره خودکار روشن بشه؟")]
    public bool restoreGeneratorOnReset = true;

    [Tooltip("فقط AutoBlackout: لحظه خاموش شدن جنراتور — برای صدا و افکت")]
    public UnityEvent OnGeneratorShutdown;

    [Header("Jumpscare")]
    [Tooltip("اختیاری: کامپوننت JumpscareTrigger خود کیت UHFPS — پر کنی، موقع جامپ‌اسکر افکت‌های کیت (وینیت/تنتاکل/لرزش دوربین) فعال میشه و قبل از ریست شب هم خودکار بسته میشه")]
    public JumpscareTrigger kitJumpscare;

    [Tooltip("افکت‌های دلخواه (فلش، لرزش، صدا و...)")]
    public UnityEvent OnJumpscare;

    [Tooltip("چند ثانیه جامپ‌اسکر بمونه بعدش شب ریست شه")]
    public float jumpscareDuration = 1.8f;

    [Tooltip("مرد خودکار بیاد جلوی صورت پلیر؟")]
    public bool teleportManOnJumpscare = true;
    public float jumpscareDistance = 0.9f;

    [Tooltip("v1.3.3: لایه‌های کف برای Raycast زیر نقطه اسپاون مرد (پیش‌فرض: همه)")]
    public LayerMask groundMask = ~0;

    [Tooltip("v1.3.3: اگه جامپ‌اسکر از داخل نمای CCTV شروع شه، چند ثانیه به زمان نمایش اضافه شه؟ (برگشت از دوربین فید داره)")]
    public float cctvReturnBuffer = 1.5f;

    [Tooltip("اختیاری: سورس صدا (2D یا روی مرد)")]
    public AudioSource eventAudio;
    public AudioClip approachClip;   // موقع ظاهر شدن
    public AudioClip screamClip;     // موقع جامپ‌اسکر

    [Header("Force Look (v1.3.4: قفل دوربین روی صورت مرد)")]
    [Tooltip("موقع جامپ‌اسکر، دوربین خودش بچرخه و قفل شه روی صورت مرد (مستقل از افکت‌های کیت — تضمین میکنه کاربر حتماً هیولا رو ببینه)")]
    public bool forceLookAtMan = true;

    [Tooltip("سرعت چرخش دوربین به سمت مرد — عدد بالا = چرخش یهویی و ناگهانی")]
    public float lookSnapSpeed = 12f;

    [Header("شرط شروع (v1.3.16: مرد تا نیفتاده توی کادر CAM راه نمی‌افته)")]
    [Tooltip("مرد فقط وقتی شروع به راه رفتن می‌کنه که پلیر روی دوربین CAM رویداد باشه و خودِ دوربین واقعاً به سمتش باشه (هیولا توی کادر دیده بشه). پلیر باید دوربین رو دستی بچرخونه تا هیولا رو پیدا کنه. اگه تیک رو برداری، با انتخاب دوربین فوراً راه می‌افته.")]
    public bool startOnlyWhenManInFrame = true;

    [Tooltip("تا وقتی مرد توی کادر نیست، هر چند ثانیه یک‌بار لاگ یادآوری بده (0 = لاگ نزن)")]
    public float waitingInFrameLogInterval = 3f;

    [Header("Green Halo Fix (v1.3.15: بیضی/هاله سبز روی CAM 5)")]
    [Tooltip("وسط رویداد، هاله‌های نورانی بزرگ (گلو چراغ‌ها، فلر لنز، پارتیکل و حتی هاله‌ی خود Spot Light دوربین CAM) که نصف صفحه رو گرفتن، موقتاً پنهان میشن — ولی افکت دید در شب (Camera Volume) هیچ‌وقت دست نمی‌خوره. آخر رویداد همه برمی‌گردن.")]
    public bool autoHideCctvGlare = true;

    [Tooltip("حداقل اندازه روی صفحه (نسبت به ارتفاع/عرض تصویر) که «هاله بزرگ» حساب بشه — پیش‌فرض 0.15 یعنی ۱۵٪ صفحه")]
    [Range(0.05f, 0.9f)] public float glareMinScreenSize = 0.15f;

    [Tooltip("دستی — رندرهایی که موقع نمای CAM «همیشه» پنهان میشن (اگه بیضی سبز رو خودکار پیدا نکرد: آبجکتش رو از Hierarchy بکش اینجا)")]
    public Renderer[] cctvGlareForceHide;

    [Tooltip("دستی — رندرهایی که «هیچ‌وقت» پنهان نمیشن (مثلاً افکت دید در شب اگه اشتباهی پنهان شد)")]
    public Renderer[] cctvGlareNeverHide;

    [Header("CCTV Light (v1.3.14: نور با خود دوربین می‌چرخه)")]
    [Tooltip("موقع شروع رویداد، اگه نورِ زیر VirtualCamera/LiveCamera دوربینِ رویداد خاموش باشه، خودکار روشن میشه. (اسپات‌لایت و افکت دید در شب فرزندِ VirtualCamera هستن و با چرخش دوربین خودکار می‌چرخن — کد جداگانه‌ای لازم ندارن)")]
    public bool ensureCctvLightOn = true;

    [Tooltip("لاگ عیب‌یابی: موقع شروع رویداد وضعیت نور/ولوم/دوربین‌های CAM رویداد لاگ میشه — بعد از حل مشکل خاموشش کن")]
    public bool cctvRigDebugLog = true;

    [Header("Events")]
    public UnityEvent OnManAppeared;    // شروع رویداد (دوربین قفل شد)
    public UnityEvent OnCameraUnlocked; // پایان قفل — پلیر می‌تونه بیاد بیرون
    public UnityEvent OnManVanished;    // نجات موفق — مرد غیب شد

    [Header("Reset (اول هر شب)")]
    [Tooltip("مرد موقع Idle مخفی باشه؟")]
    public bool hideManWhenIdle = true;

    [Tooltip("چراغ‌ها به حالت اولشون برگردن؟")]
    public bool restoreLightsOnReset = true;

    // ---------- Runtime ----------
    private Phase phase = Phase.Idle;
    private bool usedThisNight;
    private bool unlocked;
    private float phaseTime;
    private float lightsTimer;
    private float stareTime;
    private float blackoutTimer;
    private bool genRunning = false;       // وضعیت جنراتور — با ایونت‌های خودش sync میشه
                                           // (چون پلیر خودش جنراتور رو روشن میکنه، اول شب خاموشه؛
                                           //  با OnGeneratorStart خودکار true میشه)
    private bool genKilledByEvent;         // AutoBlackout: خود ایونت خاموشش کرده
    private bool genStoppedDuringEvent;    // وسط رویداد خاموش شده (پلیر یا سوخت)

    private Vector3 manStartPos;
    private Quaternion manStartRot;
    private bool walkDone;
    private bool timerWarned;             // v1.3.7 — پیش‌شماره ۵ ثانیه‌ی تایمر
    private bool stareWarned;             // v1.3.7 — لاگ یک‌باره شروع زل‌زدن
    private float lastWaitingFrameLog; // v1.3.16 — throttle لاگ «مرد هنوز توی کادر نیست»

    private Transform headBone;
    private Transform stareTargetCam;
    private bool[] initialLightEnabled;
    private bool[] initialLightGOActive;
    private NightManager nightManager;
    private Camera cachedPlayerCam;   // v1.3.2 — دوربین پلیر (اول شروع صحنه کش میشه)
    private bool exitedCctvOnJumpscare;   // v1.3.3 — جامپ‌اسکر از داخل نمای CCTV شروع شد؟

    // v1.3.9 — پنهان‌سازی موقت هاله‌ها/فلرهای بزرگ داخل نمای CAM
    private readonly List<Renderer> hiddenGlareRenderers = new List<Renderer>();
    private readonly List<Light> hiddenFlareLights = new List<Light>();
    private readonly List<Flare> hiddenFlareValues = new List<Flare>();
    private readonly List<Behaviour> hiddenFlareComponents = new List<Behaviour>();
    private readonly List<MonoBehaviour> hiddenGlowScripts = new List<MonoBehaviour>();
    private Coroutine glareScanRoutineRef;
    private bool glareRestoreLogged;
    private readonly HashSet<Renderer> protectedCandidateLogged = new HashSet<Renderer>(); // v1.3.15 — لاگ یک‌باره‌ی محافظت‌شده‌ها

    // ------------------ Unity ------------------

    private void Awake()
    {
        if (cctv == null)
            cctv = FindObjectOfType<CCTV_CameraSystem>();

        if (scareMan != null)
        {
            manStartPos = scareMan.transform.position;
            manStartRot = scareMan.transform.rotation;

            // اگر فیلد Man Animator خالی بود، خودکار روی مدل پیدا کن
            if (manAnimator == null)
            {
                manAnimator = scareMan.GetComponentInChildren<Animator>(true);
                if (manAnimator != null)
                    Debug.Log($"[Night1Scare] Animator خودکار پیدا شد روی «{manAnimator.gameObject.name}»");
            }

            if (hideManWhenIdle)
                scareMan.SetActive(false);
        }

        if (targetLights != null && targetLights.Length > 0)
        {
            initialLightEnabled = new bool[targetLights.Length];
            initialLightGOActive = new bool[targetLights.Length];
            for (int i = 0; i < targetLights.Length; i++)
            {
                if (targetLights[i] == null) continue;
                initialLightEnabled[i] = targetLights[i].enabled;
                initialLightGOActive[i] = targetLights[i].gameObject.activeSelf;
            }
        }

        // قفل گزارش وسط رویداد (نیاز به NightManager v1.1)
        NightManager.ExternalBlockCheck += IsEventBlocking;
    }

    private void Start()
    {
        nightManager = FindObjectOfType<NightManager>();
        if (nightManager != null)
            nightManager.OnNightStarted.AddListener(HandleNightStarted);

        // اتصال به جنراتور — وضعیتش رو با ایونت‌های خود جنراتور دنبال می‌کنیم
        if (useGenerator)
        {
            if (generator == null)
                generator = FindObjectOfType<PowerGenerator>();

            if (generator != null)
            {
                generator.OnGeneratorStart.AddListener(HandleGeneratorOn);
                generator.OnGeneratorEnd.AddListener(HandleGeneratorOff);
                generator.OnOutOfFuel.AddListener(HandleGeneratorOff);
            }
            else
            {
                Debug.LogWarning("[Night1Scare] جنراتور (PowerGenerator) تو صحنه پیدا نشد — نجات با جنراتور کار نمیکنه!");
            }
        }

        if ((targetLights == null || targetLights.Length == 0) && !useGenerator)
            Debug.LogWarning("[Night1Scare] هیچ چراغی تعیین نشده — شرط خاموش کردن چراغ‌ها کار نمیکنه " +
                             "و رویداد همیشه به تایمر جامپ‌اسکر می‌رسه!");

        // ─── تشخیص مشکل «مرد فقط کشیده میشه و انیمیشن نداره» ───
        if (manAnimator == null && scareMan != null)
            Debug.LogWarning("[Night1Scare] هیچ Animator ای روی مرد نیست — انیمیشن پخش نمیشه و فقط جابه‌جاش میشه!");
        else if (manAnimator != null)
        {
            if (manAnimator.runtimeAnimatorController == null)
                Debug.LogWarning($"[Night1Scare] Animator روی «{manAnimator.gameObject.name}» Controller نداره — " +
                                 "کنترلری که ساختی (ScaryManAC) رو توی فیلد Controller کامپوننت Animator بنداز!");
            else if (string.IsNullOrEmpty(walkTrigger))
                Debug.LogWarning("[Night1Scare] فیلد Walk Trigger خالیه — انیمیشن راه‌رفتن اجرا نمیشه! (توی Inspector دقیقاً بنویس: Walk)");
        }

        // v1.3.2 — دوربین پلیر رو همین اول کار کش کن (وقتی پلیر توی نمای CCTVـه
        // یا وسط فید برگشته، Camera.main ممکنه null برگردونه)
        GetPlayerCamera();
    }

    // v1.3.2 — دوربین پلیر رو مطمئن پیدا میکنه
    private Camera GetPlayerCamera()
    {
        if (cachedPlayerCam != null)
            return cachedPlayerCam;

        // 1) راه استاندارد
        cachedPlayerCam = Camera.main;

        // 2) هر دوربین فعالی که اجدادش CharacterController دارن = دوربین پلیر
        if (cachedPlayerCam == null)
        {
            Camera[] all = Camera.allCameras;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null || !all[i].enabled) continue;
                if (all[i].GetComponentInParent<CharacterController>() != null)
                {
                    cachedPlayerCam = all[i];
                    break;
                }
            }
        }

        // 3) هر دوربین فعال تگ‌خورده با MainCamera
        if (cachedPlayerCam == null)
        {
            Camera[] all = Camera.allCameras;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].enabled && all[i].CompareTag("MainCamera"))
                {
                    cachedPlayerCam = all[i];
                    break;
                }
            }
        }

        if (cachedPlayerCam != null)
            Debug.Log($"[Night1Scare] دوربین پلیر پیدا شد: «{cachedPlayerCam.name}»");

        return cachedPlayerCam;
    }

    private void HandleGeneratorOn() => genRunning = true;

    private void HandleGeneratorOff()
    {
        genRunning = false;
        if (phase == Phase.WaitingLights || phase == Phase.Vanishing)
            genStoppedDuringEvent = true;
    }

    private void OnDestroy()
    {
        NightManager.ExternalBlockCheck -= IsEventBlocking;
        RestoreHiddenGlare();          // v1.3.9 — محکم‌کاری: هاله‌ها برگردن
    }

    private void Update()
    {
        switch (phase)
        {
            case Phase.Idle:
                TryTrigger();
                break;

            case Phase.Walking:
                UpdateWalking();
                break;

            case Phase.WaitingLights:
                UpdateWaitingLights();
                break;

            case Phase.Vanishing:
                phaseTime += Time.deltaTime;
                if (phaseTime >= vanishDelay)
                {
                    if (scareMan != null)
                        scareMan.SetActive(false);

                    phase = Phase.Done;
                    Debug.Log("[Night1Scare] مرد غیب شد — رویداد با موفقیت تموم شد.");
                    OnManVanished?.Invoke();
                }
                break;
        }
    }

    // ------------------ فازها ------------------

    private void TryTrigger()
    {
        if (oncePerNight && usedThisNight) return;
        if (NightManager.CurrentNight != nightNumber) return;
        if (cctv == null || scareMan == null) return;

        // پلیر الان داره دوربین موردنظر رو می‌بینه؟
        if (cctv.IsCameraView && cctv.CurrentCameraIndex == scareCameraNumber - 1)
        {
            // v1.3.16 — مرد تا وقتی خودِ دوربین به سمتش نیست، راه نمی‌افته
            if (!startOnlyWhenManInFrame || IsManInScareCamFrame())
                StartEvent();
            else
                LogWaitingForManInFrame();
        }
    }

    private void StartEvent()
    {
        usedThisNight = true;
        phase = Phase.Walking;
        phaseTime = 0f;
        unlocked = false;
        stareTime = 0f;
        lightsTimer = 0f;
        protectedCandidateLogged.Clear(); // v1.3.15 — لاگ محافظت‌شده‌ها از نو

        // مرد از نقطه اولیه شروع کنه
        scareMan.transform.SetPositionAndRotation(manStartPos, manStartRot);
        scareMan.SetActive(true);

        walkDone = false;
        headBone = FindHeadBone();
        stareTargetCam = GetScareCameraTransform();

        // v1.3.14 — مطمئن شیم نور دوربین رویداد روشنه (نور فرزندِ VirtualCameraه و باهاش می‌چرخه)
        if (ensureCctvLightOn)
            EnsureCctvLightOnForScareCam();

        if (manAnimator != null)
        {
            if (!string.IsNullOrEmpty(walkTrigger))
            {
                manAnimator.SetTrigger(walkTrigger);
                Debug.Log($"[Night1Scare] SetTrigger(\"{walkTrigger}\") فرستاده شد — انیمیشن راه‌رفتن");
            }
            else
                Debug.LogWarning("[Night1Scare] فیلد Walk Trigger خالیه — SetTrigger فرستاده نشد!");
        }

        if (eventAudio != null && approachClip != null)
            eventAudio.PlayOneShot(approachClip);

        // قفل کنترل دوربین
        cctv.SetControlLocked(true);

        // v1.3.9 — شروع اسکن هاله‌های بزرگ داخل نمای دوربین (بیضی سبز)
        if (autoHideCctvGlare)
        {
            if (glareScanRoutineRef != null) StopCoroutine(glareScanRoutineRef);
            glareScanRoutineRef = StartCoroutine(GlareScanRoutine());
        }

        Debug.Log($"[Night1Scare] رویداد شروع شد (CAM {scareCameraNumber}) — قفل {lockDuration}s، راه‌رفتن {walkDuration}s");
        OnManAppeared?.Invoke();
    }

    private void UpdateWalking()
    {
        phaseTime += Time.deltaTime;

        // راه‌رفتن قدم قدم سمت دوربین
        if (scareTargetPoint != null && phaseTime <= walkDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(phaseTime / walkDuration));
            scareMan.transform.position = Vector3.Lerp(manStartPos, scareTargetPoint.position, t);

            Transform camT = GetScareCameraTransform();
            if (camT != null)
            {
                Vector3 dir = camT.position - scareMan.transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    scareMan.transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        // پایان راه‌رفتن → مرد یهو به دوربین نگاه می‌کنه
        if (!walkDone && phaseTime >= walkDuration)
        {
            walkDone = true;
            if (manAnimator != null)
            {
                if (!string.IsNullOrEmpty(lookTrigger))
                    manAnimator.SetTrigger(lookTrigger);
                else
                    Debug.LogWarning("[Night1Scare] فیلد Look Trigger خالیه — انیمیشن نگاه به دوربین اجرا نمیشه!");
            }
            Debug.Log("[Night1Scare] راه‌رفتن تمام شد — مرد به دوربین نگاه می‌کنه.");
        }

        // پایان قفل — پلیر می‌تونه بیاد بیرون
        if (!unlocked && phaseTime >= lockDuration)
        {
            unlocked = true;
            cctv.SetControlLocked(false);
            Debug.Log("[Night1Scare] قفل دوربین باز شد — حالا باید چراغ‌ها رو خاموش کنه!");
            OnCameraUnlocked?.Invoke();
        }

        // فاز بعد
        if (phaseTime >= Mathf.Max(walkDuration, lockDuration))
        {
            phase = Phase.WaitingLights;
            phaseTime = 0f;
            lightsTimer = 0f;
            stareTime = 0f;
            blackoutTimer = 0f;
            timerWarned = false;
            stareWarned = false;
            // v1.3.7: لاگ وضعیت فاز انتظار — اگه بعد از این لاگ هیچی نشد، دقیقاً معلومه کدوم مکانیزم غیرفعاله
            string timerState = useLightsTimer ? $"فعال ({lightsOffTimeLimit:0.#}s)" : "غیرفعال!";
            string stareState = useStareDetection
                ? $"فعال ({stareTimeLimit:0.#}s)" + (stareWorksInCctvView ? " + از پشت مانیتور" : "")
                : "غیرفعال!";
            Debug.Log($"[Night1Scare] فاز انتظار شروع شد — تایمر: {timerState} | زل‌زدن: {stareState} | پلیر باید چراغ‌ها/جنراتور رو خاموش کنه تا نجات پیدا کنه");
        }
    }

    // سر زنده — بعد از رسیدن، سر مرد همیشه دنبال لنز دوربین می‌مونه
    // (v1.3.16 — تعقیب خودکار دوربین کلاً حذف شد؛ دوربین فقط با دست پلیر می‌چرخه)
    private void LateUpdate()
    {
        if (!headLookAt || headBone == null || stareTargetCam == null) return;
        if (scareMan == null || !scareMan.activeSelf) return;

        bool manStanding = phase == Phase.WaitingLights || phase == Phase.Vanishing
                           || (phase == Phase.Walking && walkDone);
        if (!manStanding) return;

        Quaternion headRot = Quaternion.LookRotation(stareTargetCam.position - headBone.position);
        headBone.rotation = Quaternion.Slerp(headBone.rotation, headRot, headSnapSpeed * Time.deltaTime);
    }

    private void UpdateWaitingLights()
    {
        bool lightsOff = AreLightsOff();

        // حالت AutoBlackout: خود ایونت بعد از چند ثانیه جنراتور رو خاموش می‌کنه
        if (useGenerator && !genKilledByEvent
            && generatorMode == GeneratorScareMode.AutoBlackout
            && generator != null)
        {
            blackoutTimer += Time.deltaTime;
            if (blackoutTimer >= blackoutDelay)
            {
                genKilledByEvent = true;
                generator.StopGenerator();   // همه چراغ‌ها خودکار خاموش میشن
                Debug.Log("[Night1Scare] جنراتور خاموش شد — تاریکی ناگهانی!");
                OnGeneratorShutdown?.Invoke();
            }
        }

        bool generatorOff = useGenerator && (!genRunning || genKilledByEvent);

        // فیل 1: تایمر — جنراتور/چراغ‌ها سر وقت خاموش نشدن
        if (useLightsTimer)
        {
            lightsTimer += Time.deltaTime;

            // v1.3.7: پیش‌شماره — ۵ ثانیه مونده به جامپ‌اسکر
            if (!timerWarned && lightsTimer >= lightsOffTimeLimit - 5f)
            {
                timerWarned = true;
                Debug.Log("[Night1Scare] ۵ ثانیه به جامپ‌اسکر مونده — چراغ‌ها رو خاموش کن!");
            }

            if (lightsTimer >= lightsOffTimeLimit)
            {
                Debug.Log("[Night1Scare] جنراتور/چراغ‌ها سر وقت خاموش نشدن — جامپ‌اسکر!");
                DoJumpscare();
                return;
            }
        }

        // فیل 2: زل زدن به مرد (وقتی چراغ‌ها روشنه)
        // v1.3.7: از پشت مانیتور هم کار میکنه (Stare Works In Cctv View) —
        // مرد که جلوی CAM 5 وایساده و داره بهت زل میزنه، اگه ادامه بدی نگاهش کنی میاد سراغت!
        if (useStareDetection && !lightsOff && !generatorOff && cctv != null
            && (!cctv.IsCameraView || stareWorksInCctvView))
        {
            if (IsPlayerStaring())
            {
                if (!stareWarned && stareTime <= 0f)
                {
                    stareWarned = true;
                    Debug.Log($"[Night1Scare] داری به مرد زل می‌زنی{(cctv.IsCameraView ? " (از پشت مانیتور!)" : "")} — {stareTimeLimit:0.#}s تا جامپ‌اسکر... یا واذه یا چراغ‌ها رو خاموش کن!");
                }
                stareTime += Time.deltaTime;
            }
            else
            {
                stareTime = 0f;
                stareWarned = false;
            }

            if (stareTime >= stareTimeLimit)
            {
                Debug.Log("[Night1Scare] پلیر به مرد زل زد — جامپ‌اسکر!");
                DoJumpscare();
                return;
            }
        }

        // موفقیت: جنراتور خاموش شد یا چراغ‌ها خاموش شدن → شمارش معکوس تا غیب شدن
        if (generatorOff || lightsOff)
        {
            phase = Phase.Vanishing;
            phaseTime = 0f;
            Debug.Log($"[Night1Scare] تاریکی افتاد — {vanishDelay}s تا غیب شدن مرد...");
        }
    }

    private void DoJumpscare()
    {
        if (phase == Phase.Jumpscaring) return;
        phase = Phase.Jumpscaring;

        // اگه وسط نمای دوربینه، اول بیار بیرون
        exitedCctvOnJumpscare = false;
        if (cctv != null)
        {
            cctv.SetControlLocked(false);
            if (cctv.IsCameraView)
            {
                cctv.SwitchBack();
                exitedCctvOnJumpscare = true;   // v1.3.3: برگشت از CCTV فید داره → به زمان نمایش اضافه میشه
            }
        }

        // مرد بیاد جلوی صورت پلیر (v1.3.2: دوربین کش‌شده + جهت افقی + لاگ تشخیصی)
        if (!teleportManOnJumpscare)
            Debug.LogWarning("[Night1Scare] «Teleport Man On Jumpscare» تیک نخورده — مرد جلوی صورت پلیر نمیاد! توی Inspector تیکش رو بزن.");

        if (teleportManOnJumpscare && scareMan == null)
            Debug.LogWarning("[Night1Scare] فیلد Scare Man خالیه — تلپورت مرد انجام نشد!");

        if (teleportManOnJumpscare && scareMan != null)
        {
            Camera cam = GetPlayerCamera();
            if (cam != null)
            {
                // v1.3.10 — اگه جامپ‌اسکر از داخل نمای CCTV شروع شده، پوز لحظه‌ای دوربین اصلی
                // هنوز همون پوز CAM روی برجه (۹-۱۰ متر اون بالا!) — مرد اونجا اسپاون میشد و
                // پلیر هیچیش نمیدید. پس مبنای اسپاون = ریگ خود پلیر (جای پا/تن پلیر)،
                // چون ریگ پلیر وسط نمای CCTV هم سر جاش پشت مانیتور وایساده.
                CharacterController cc = cam.GetComponentInParent<CharacterController>();
                bool useRig = exitedCctvOnJumpscare && cc != null;

                Vector3 origin = useRig ? cc.transform.position : cam.transform.position;
                Vector3 originFwd = useRig ? cc.transform.forward : cam.transform.forward;

                // جهت افقی نگاه (نگاه بالا/پایین تاثیری نداشته باشه)
                Vector3 fwd = originFwd;
                fwd.y = 0f;
                if (fwd.sqrMagnitude < 0.0001f)
                {
                    // پلیر تقریباً عمود نگاه میکنه — از جهت خود مرد استفاده کن
                    fwd = scareMan.transform.position - origin;
                    fwd.y = 0f;
                }
                fwd = fwd.sqrMagnitude > 0.0001f ? fwd.normalized : Vector3.forward;

                Vector3 pos = origin + fwd * jumpscareDistance;

                // v1.3.3: کف واقعی زیر نقطه اسپاون پیدا شه — مرد با کفِ اتاق دوربینِ CAP 5
                // اسپاون نمیشه (توی برج چندطبقه باعث میشد زیر کف/داخل سقف اسپاون شه!)
                float manY = scareMan.transform.position.y;
                if (Physics.Raycast(pos + Vector3.up * 0.5f, Vector3.down, out RaycastHit groundHit, 30f, groundMask, QueryTriggerInteraction.Ignore))
                {
                    manY = groundHit.point.y;
                }
                else
                {
                    Debug.LogWarning("[Night1Scare] کف زیر نقطه اسپاون پیدا نشد — از ارتفاع قبلی مرد استفاده میشه.");
                }
                pos.y = manY;
                scareMan.transform.position = pos;

                Vector3 dir = origin - scareMan.transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    scareMan.transform.rotation = Quaternion.LookRotation(dir);

                scareMan.SetActive(true);

                string anchorName = useRig
                    ? "ریگ پلیر (اسپاون از داخل نمای CCTV)"
                    : $"دوربین «{cam.name}»";
                Debug.Log($"[Night1Scare] مرد جلوی {anchorName} تلپورت شد " +
                          $"(فاصله {jumpscareDistance}م، مبنای اسپاون Y={origin.y:0.00}، کف مرد Y={manY:0.00}، موقعیت: {pos}).");

                if (manAnimator != null && !string.IsNullOrEmpty(screamTrigger))
                    manAnimator.SetTrigger(screamTrigger);
            }
            else
            {
                Debug.LogWarning("[Night1Scare] دوربین پلیر هیچ‌جوره پیدا نشد — تلپورت مرد انجام نشد! " +
                    "درست کن: دوربین پلیر (فرزند FPS Controller) رو انتخاب کن و Tagش رو بذار MainCamera.");
            }
        }

        // v1.3.4: دوربین قفل شه روی صورت مرد — مهم نیست کیت چی‌کار میکنه،
        // خودمون تضمین میکنیم کاربر حتماً هیولا رو ببینه
        if (forceLookAtMan && scareMan != null)
            StartCoroutine(ForceLookAtManRoutine());

        if (eventAudio != null && screamClip != null)
            eventAudio.PlayOneShot(screamClip);

        // افکت‌های جامپ‌اسکر خود کیت (وینیت/تنتاکل/لرزش دوربین) — اگه وصل شده باشه
        // v1.3.1: اول مرجع خالی کیت (Look At Target) خودکار پر میشه، بعد با try/catch
        // صدا زده میشه تا خطای کیت هیچ‌وقت جلوی ریست شب رو نگیره.
        if (kitJumpscare != null)
        {
            AutoFixKitLookTarget();

            try
            {
                kitJumpscare.TriggerJumpscare();
                Debug.Log("[Night1Scare] افکت‌های JumpscareTrigger خود کیت فعال شد.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    "[Night1Scare] JumpscareTrigger کیت خطا داد و افکت‌های کیت رد شد — " +
                    "ولی جامپ‌اسکر خودمون و ریست شب ادامه پیدا میکنه. جزئیات: " + ex.Message);
            }
        }

        Debug.Log("[Night1Scare] جامپ‌اسکر! — باخت و ریست شب...");
        OnJumpscare?.Invoke();
        StartCoroutine(JumpscareRoutine());
    }

    private IEnumerator JumpscareRoutine()
    {
        GameManager gm = GameManager.Instance;

        // v1.3.3: اگه از داخل نمای CCTV اومدیم بیرون، فیدِ برگشت زمان میبره —
        // بدون این بافر، فیدِ سیاهِ ریست قبل از دیدنِ مرد میاد!
        float visibleTime = jumpscareDuration;
        if (exitedCctvOnJumpscare)
        {
            visibleTime += Mathf.Max(0f, cctvReturnBuffer);
            Debug.Log($"[Night1Scare] برگشت از نمای CCTV — {Mathf.Max(0f, cctvReturnBuffer):0.0}s به زمان نمایش جامپ‌اسکر اضافه شد.");
        }

        // مطمئن شو پلیر کاملاً فریزه (SwitchBack ممکنه با تأخیر آزادش کنه)
        yield return new WaitForSeconds(0.1f);
        if (gm != null) gm.FreezePlayer(true);

        yield return new WaitForSeconds(0.4f);
        if (gm != null) gm.FreezePlayer(true);

        yield return new WaitForSeconds(Mathf.Max(0.1f, visibleTime - 0.5f));

        // اگه ترنزیشن دیگه‌ای وسط کار بود، صبرش کن
        float wait = 0f;
        while (nightManager != null && nightManager.IsTransitioning && wait < 5f)
        {
            wait += Time.deltaTime;
            yield return null;
        }

        // افکت‌های کیت رو قبل از ریست ببند (تا بعد از لود صحنه نمونن)
        // برای این کار End Jumpscare With Event باید توی کامپوننت کیت تیک بخوره
        if (kitJumpscare != null)
        {
            try { kitJumpscare.TriggerJumpscareEnded(); }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[Night1Scare] بستن افکت کیت خطا داد: " + ex.Message);
            }
        }

        if (nightManager != null)
            nightManager.RestartCurrentNight();
        else
            Debug.LogWarning("[Night1Scare] NightManager پیدا نشد — ریست شب انجام نشد!");
    }

    // v1.3.5 — دوربین رو می‌چرخونه و تا آخر جامپ‌اسکر قفل میکنه روی صورت مرد
    // نکته: دوربین اصلی UHFPS رو Cinemachine Brain کنترل میکنه — چرخش مستقیم خود دوربین
    // بازنویسی میشه. راه درست: چرخوندن «ریگ نگاه» + استفاده از LookController خود کیت.
    private IEnumerator ForceLookAtManRoutine()
    {
        Camera cam = GetPlayerCamera();
        if (cam == null || scareMan == null) yield break;

        Transform face = ResolveHeadTransform();
        if (face == null) yield break;

        // اگه به جای استخوان سر، خود مدل برگشت (پای مرد)، نقطه‌ی حدودی صورت رو بساز
        bool faceIsRoot = (face == scareMan.transform);

        // ریگ نگاه: بالاترین جدِ دوربین که مستقیماً زیر ریشه‌ی پلیر (CharacterController) هست
        Transform topRig = cam.transform;
        CharacterController cc = cam.GetComponentInParent<CharacterController>();
        if (cc != null)
        {
            Transform t = cam.transform;
            while (t.parent != null && t.parent != cc.transform)
                t = t.parent;
            if (t != cc.transform)
                topRig = t;
        }

        // مسیر اصلی (v1.3.6): CinemachineBrain موقتاً خاموش بشه تا هیچی چرخش دستی رو پاک نکنه
        Behaviour camBrain = DisableCinemachineBrain(cam);

        // مسیر 1: LookController خود کیت (پیدا کردن با اسم کامپوننت — بدون وابستگی کامپایل)
        Component kitLook = FindKitLookController();
        System.Reflection.MethodInfo kitLerp = null;
        if (kitLook != null)
        {
            kitLerp = kitLook.GetType().GetMethod("LerpRotation",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            if (kitLerp == null)
                Debug.LogWarning("[Night1Scare] LookController کیت پیدا شد ولی متد LerpRotation نداشت — فقط ریگ می‌چرخه.");
        }
        else
        {
            Debug.LogWarning("[Night1Scare] LookController کیت روی پلیر پیدا نشد — فقط ریگ می‌چرخه.");
        }

        // حداقل به اندازه‌ی کل پنجره‌ی نمایش جامپ‌اسکر قفل بمونه
        float hold = jumpscareDuration
                   + (exitedCctvOnJumpscare ? Mathf.Max(0f, cctvReturnBuffer) : 0f)
                   + 0.5f;

        Debug.Log($"[Night1Scare] دوربین قفل شد روی صورت مرد (Force Look — ریگ: «{topRig.name}»).");

        float timer = 0f;
        float nextKitCall = 0f;
        while (timer < hold && phase == Phase.Jumpscaring &&
               cam != null && face != null && scareMan != null)
        {
            timer += Time.deltaTime;

            // نقطه‌ی هدف: استخوان سر (زنده — با انیمیشن جیغ تکون میخوره) یا نقطه‌ی حدودی صورت
            Vector3 target = faceIsRoot
                ? scareMan.transform.position + Vector3.up * 1.6f
                : face.position;

            Quaternion look = Quaternion.LookRotation(target - cam.transform.position, Vector3.up);

            // مسیر 1 — کیت: هر 0.75s دوباره صدا زده میشه تا اگه فید CCTV افکتش رو خورد، ترمیم شه
            if (kitLerp != null && timer >= nextKitCall)
            {
                nextKitCall = timer + 0.75f;
                try
                {
                    kitLerp.Invoke(kitLook, new object[] { face, 0.2f, true });
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[Night1Scare] LerpRotation کیت خطا داد: " + ex.Message);
                    kitLerp = null;
                }
            }

            // مسیر 2 — ریگ: دلتای چرخش تا نشانه‌گیری روی صورت، روی بالاترین گره‌ی ریگ،
            // تا اگه اسکریپت نگاه کیت ریگ رو تکون بده، ریگ هم هم‌راستا با هدف بمونه
            Quaternion delta = look * Quaternion.Inverse(cam.transform.rotation);
            delta = Quaternion.Slerp(Quaternion.identity, delta, Mathf.Min(1f, Time.deltaTime * lookSnapSpeed));

            if (topRig != cam.transform)
                topRig.rotation = delta * topRig.rotation;

            // مسیر 3 (v1.3.6 — اصلی): چرخش «مطلق» خود دوربین — چون Brain خاموشه،
            // دیگه هیچی این چرخش رو پاک نمیکنه و دوربین واقعاً روی صورت مرد قفل میمونه
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, look,
                Mathf.Min(1f, Time.deltaTime * lookSnapSpeed));

            yield return null;
        }

        // v1.3.6 — آخر کار Brain رو دوباره روشن کن (بعدش معمولاً صحنه ریست میشه، ولی محکم‌کاری)
        if (camBrain != null)
        {
            camBrain.enabled = true;
            Debug.Log("[Night1Scare] CinemachineBrain دوباره روشن شد.");
        }
    }

    // v1.3.6 — پیدا کردن و خاموش کردن CinemachineBrain روی دوربین (با اسم کامپوننت — بدون وابستگی کامپایل)
    // هم Cinemachine 2.x و هم 3.x اسم کامپوننت رو CinemachineBrain گذاشتن
    private Behaviour DisableCinemachineBrain(Camera cam)
    {
        if (cam == null) return null;

        Component[] comps = cam.GetComponents<Component>();
        for (int i = 0; i < comps.Length; i++)
        {
            Component c = comps[i];
            if (c == null) continue;

            if (c.GetType().Name.Contains("CinemachineBrain"))
            {
                Behaviour b = c as Behaviour;
                if (b != null && b.enabled)
                {
                    b.enabled = false;
                    Debug.Log($"[Night1Scare] CinemachineBrain روی «{cam.name}» موقتاً خاموش شد — چرخش Force Look دیگه بازنویسی نمیشه.");
                }
                return b;
            }
        }

        Debug.LogWarning("[Night1Scare] روی دوربین CinemachineBrain پیدا نشد — چرخش مستقیم امتحان میشه.");
        return null;
    }

    // v1.3.5 — پیدا کردن LookController کیت با اسم کامپوننت (بدون رفرنس کامپایلی)
    private Component FindKitLookController()
    {
        MonoBehaviour[] all = FindObjectsOfType<MonoBehaviour>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].GetType().Name == "LookController")
                return all[i];
        }
        return null;
    }

    // ------------------ کمکی ------------------

    private bool AreLightsOff()
    {
        if (targetLights == null || targetLights.Length == 0) return false;

        int count = 0, offCount = 0;
        foreach (Light l in targetLights)
        {
            if (l == null) continue;
            count++;
            bool isOn = l.enabled && l.gameObject.activeInHierarchy;
            if (!isOn) offCount++;
        }

        if (count == 0) return false;
        return requireAllLightsOff ? offCount == count : offCount > 0;
    }

    private Transform FindHeadBone()
    {
        if (!headLookAt || scareMan == null || string.IsNullOrEmpty(headBoneName)) return null;

        string key = headBoneName.Trim().ToLower();
        Transform[] all = scareMan.GetComponentsInChildren<Transform>();
        for (int i = 0; i < all.Length; i++)
            if (all[i].name.ToLower().Contains(key))
                return all[i];

        Debug.LogWarning($"[Night1Scare] استخوان سر «{headBoneName}» پیدا نشد — فقط بدن به دوربین نگاه می‌کنه.");
        return null;
    }

    // v1.3.1 — اگه Look At Target کامپوننت کیت خالی بود، خودکار با استخوان سر مرد پرش میکنه
    private void AutoFixKitLookTarget()
    {
        try
        {
            if (kitJumpscare.LookAtTarget == null)
            {
                Transform head = ResolveHeadTransform();
                if (head != null)
                {
                    kitJumpscare.LookAtTarget = head;
                    Debug.Log($"[Night1Scare] Look At Target کیت خالی بود → خودکار به «{head.name}» وصل شد.");
                }
                else
                {
                    Debug.LogWarning("[Night1Scare] Look At Target کیت خالیه و سر مرد هم پیدا نشد — " +
                        "توی Inspector دستی استخوان سر مرد (مثلاً mixamorig:Head) رو بکش توی فیلد Look At Target.");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Night1Scare] پر کردن خودکار Look At Target کیت ممکن نشد: " + ex.Message);
        }
    }

    // بهترین کاندید برای «سر» مرد — برای Look At Target کامپوننت کیت
    private Transform ResolveHeadTransform()
    {
        if (scareMan == null) return null;

        // 1) همون استخوانی که برای Head Look پیدا میشه (فیلد Head Bone Name)
        Transform t = FindHeadBone();
        if (t != null) return t;

        // 2) ریگ Humanoid → استخوان سر مستقیم از Animator
        if (manAnimator != null)
        {
            Transform bone = manAnimator.GetBoneTransform(HumanBodyBones.Head);
            if (bone != null) return bone;
        }

        // 3) جستجوی نامی بین همه‌ی فرزندها (حتی مخفی‌ها)
        if (!string.IsNullOrEmpty(headBoneName))
        {
            string key = headBoneName.Trim().ToLower();
            Transform[] all = scareMan.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name.ToLower().Contains(key))
                    return all[i];
        }

        // 4) آخرین راه: خود مدل — بهتر از کرش کردن
        return scareMan.transform;
    }

    private bool IsPlayerStaring()
    {
        if (scareMan == null) return false;

        Camera cam = Camera.main;
        if (cam == null) return false;

        Vector3 toMan = scareMan.transform.position - cam.transform.position;
        if (toMan.magnitude > stareMaxDistance) return false;

        return Vector3.Dot(cam.transform.forward, toMan.normalized) >= stareDotThreshold;
    }

    private CCTV_Camera GetScareCctvCamera()
    {
        if (cctv == null || cctv.Cameras == null || cctv.Cameras.Length == 0) return null;
        int idx = Mathf.Clamp(scareCameraNumber - 1, 0, cctv.Cameras.Length - 1);
        return cctv.Cameras[idx];
    }

    // ------------------ v1.3.16: شروع شرطی — مرد تا نیفتاده توی کادر راه نمی‌افته ------------------

    /// <summary>دوربینِ نمای رویداد (اول VirtualCamera که نمای تمام‌صفحه رو میده، بعد LiveCamera)</summary>
    private Camera GetScareViewCamera()
    {
        CCTV_Camera cam = GetScareCctvCamera();
        if (cam == null) return null;

        if (cam.VirtualCamera != null)
        {
            Camera vc = cam.VirtualCamera.GetComponent<Camera>();
            if (vc != null) return vc;
        }
        return cam.LiveCamera;
    }

    /// <summary>آیا جای مرد (حتی وقتی مخفیه) داخل کادرِ دوربین CAM رویداده؟
    /// جعبه‌ی حدسی ~۲ متر قد دور از موقعیتش می‌سازیم و با frustum دوربین تست می‌کنیم.</summary>
    private bool IsManInScareCamFrame()
    {
        Camera cam = GetScareViewCamera();
        if (cam == null) return true;   // دوربین پیدا نشد — مثل قبل با انتخاب دوربین شروع شه

        Vector3 pos = scareMan.transform.position + Vector3.up * 1f;
        Bounds b = new Bounds(pos, new Vector3(1.2f, 2f, 1.2f));

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
        if (planes == null || planes.Length == 0) return true;

        return GeometryUtility.TestPlanesAABB(planes, b);
    }

    private void LogWaitingForManInFrame()
    {
        if (waitingInFrameLogInterval <= 0f) return;
        if (Time.unscaledTime - lastWaitingFrameLog < waitingInFrameLogInterval) return;
        lastWaitingFrameLog = Time.unscaledTime;
        Debug.Log($"[Night1Scare] ⏳ پلیر روی CAM {scareCameraNumber} هست ولی مرد هنوز توی کادر دوربین نیست — " +
                  "تا وقتی دوربین به سمت هیولا نچرخه، راه نمی‌افته.");
    }

    // ------------------ v1.3.14: نور و افکت دوربین CAM ------------------
    // ساختار ریک (طبق اسکرین‌شات کاربر — برای همه‌ی دوربین‌ها یکسانه):
    //   .../Camera/{ LiveCamera , Virtual Camera/{ Camera Volume , Spot Light } }
    // یعنی Spot Light و Camera Volume هر دو فرزندِ Virtual Camera هستن و با دوربین می‌چرخن.
    // (v1.3.16 — تعقیب خودکار حذف شد؛ دوربین فقط با چرخوندن دستی پلیر می‌چرخه)

    /// <summary>شروع رویداد: اگه نورِ زیر VirtualCamera/LiveCamera دوربین رویداد خاموش باشه روشن میشه + وضعیتش لاگ میشه.</summary>
    private void EnsureCctvLightOnForScareCam()
    {
        CCTV_Camera cam = GetScareCctvCamera();
        if (cam == null) return;

        List<Light> rigLights = new List<Light>();
        if (cam.VirtualCamera != null)
            rigLights.AddRange(cam.VirtualCamera.GetComponentsInChildren<Light>(true));
        if (cam.LiveCamera != null)
            rigLights.AddRange(cam.LiveCamera.GetComponentsInChildren<Light>(true));

        for (int i = 0; i < rigLights.Count; i++)
        {
            Light l = rigLights[i];
            if (l == null) continue;

            if (!l.enabled || !l.gameObject.activeSelf)
            {
                l.enabled = true;
                l.gameObject.SetActive(true);
                Debug.Log($"[Night1Scare] 💡 نور «{GetObjectPath(l.transform)}» خاموش بود — برای رویداد روشن شد.");
            }
        }

        if (!cctvRigDebugLog) return;

        if (rigLights.Count == 0)
            Debug.LogWarning("[Night1Scare] 💡⚠️ هیچ نوری زیر VirtualCamera/LiveCamera دوربین رویداد پیدا نشد!");
        for (int i = 0; i < rigLights.Count; i++)
        {
            Light l = rigLights[i];
            if (l == null) continue;
            Debug.Log($"[Night1Scare] 💡 نور دوربین: «{GetObjectPath(l.transform)}» | enabled={l.enabled} " +
                      $"| فعال در صحنه={l.gameObject.activeInHierarchy} | type={l.type} " +
                      $"| intensity={l.intensity:0.##} | range={l.range:0.#} | spotAngle={l.spotAngle:0.#}");
        }
    }

    [ContextMenu("Debug: CAM Rig State (وضعیت کامل ریک دوربین رویداد)")]
    private void DebugLogCctvRigState()
    {
        CCTV_Camera cam = GetScareCctvCamera();
        if (cam == null) { Debug.Log("[Night1Scare] 🔎 دوربین رویداد پیدا نشد (CCTV خالیه؟)"); return; }

        Debug.Log($"[Night1Scare] 🔎 ===== وضعیت ریک CAM {scareCameraNumber} ===== " +
                  $"| داخل نمای دوربین؟ {cctv.IsCameraView} | دوربین فعلی: CAM {cctv.CurrentCameraIndex + 1}");

        if (cam.VirtualCamera != null)
        {
            GameObject go = cam.VirtualCamera.gameObject;
            Camera vc = cam.VirtualCamera.GetComponent<Camera>();
            string camComp = (vc != null)
                ? $"✅ enabled={vc.enabled} | depth={vc.depth} | cullingMask={vc.cullingMask}"
                : "ندارد!";
            Debug.Log($"[Night1Scare] 🔎 VirtualCamera: «{GetObjectPath(go.transform)}» | GO فعال={go.activeSelf} " +
                      $"(در صحنه: {go.activeInHierarchy}) | کامپوننت Camera: {camComp}");
        }
        else
            Debug.LogWarning("[Night1Scare] 🔎 فیلد VirtualCamera این دوربین خالیه!");

        if (cam.LiveCamera != null)
        {
            GameObject go = cam.LiveCamera.gameObject;
            string tex = cam.LiveCamera.targetTexture != null ? "وصل ✅" : "null ⚠️";
            Debug.Log($"[Night1Scare] 🔎 LiveCamera: «{GetObjectPath(go.transform)}» | GO فعال={go.activeSelf} " +
                      $"(در صحنه: {go.activeInHierarchy}) | enabled={cam.LiveCamera.enabled} | targetTexture={tex}");
        }
        else
            Debug.LogWarning("[Night1Scare] 🔎 فیلد LiveCamera این دوربین خالیه!");

        // افکت دید در شب (Camera Volume — فرزند VirtualCamera)
        if (cam.VirtualCamera != null)
        {
            UnityEngine.Rendering.Volume vol = cam.VirtualCamera.GetComponentInChildren<UnityEngine.Rendering.Volume>(true);
            if (vol != null)
            {
                string prof = vol.sharedProfile != null ? vol.sharedProfile.name : "خالی ⚠️";
                Debug.Log($"[Night1Scare] 🔎 Volume (افکت دید در شب): «{GetObjectPath(vol.transform)}» | GO فعال={vol.gameObject.activeSelf} " +
                          $"(در صحنه: {vol.gameObject.activeInHierarchy}) | enabled={vol.enabled} | weight={vol.weight:0.##} | " +
                          $"isGlobal={vol.isGlobal} | profile={prof}");
            }
            else
                Debug.LogWarning("[Night1Scare] 🔎 هیچ Volume ای زیر VirtualCamera پیدا نشد (افکت دید در شب کجاست؟!)");
        }

        // نورهای ریک
        Light[] lights = (cam.VirtualCamera != null)
            ? cam.VirtualCamera.GetComponentsInChildren<Light>(true)
            : new Light[0];
        if (lights.Length == 0 && cam.LiveCamera != null)
            lights = cam.LiveCamera.GetComponentsInChildren<Light>(true);

        if (lights.Length == 0)
            Debug.LogWarning("[Night1Scare] 🔎 هیچ نوری زیر VirtualCamera/LiveCamera پیدا نشد!");
        for (int i = 0; i < lights.Length; i++)
        {
            Light l = lights[i];
            if (l == null) continue;
            Debug.Log($"[Night1Scare] 🔎 نور[{i}]: «{GetObjectPath(l.transform)}» | enabled={l.enabled} | GO فعال={l.gameObject.activeSelf} " +
                      $"(در صحنه: {l.gameObject.activeInHierarchy}) | type={l.type} | intensity={l.intensity:0.##} | " +
                      $"range={l.range:0.#} | spotAngle={l.spotAngle:0.#} | cullingMask={l.cullingMask}");
        }

        if (cctv.CCTVEffects != null)
            Debug.Log($"[Night1Scare] 🔎 CCTVEffects (ولوم کیت): enabled={cctv.CCTVEffects.enabled} | GO فعال در صحنه={cctv.CCTVEffects.gameObject.activeInHierarchy}");

        Debug.Log("[Night1Scare] 🔎 ===== پایان وضعیت ریک =====");
    }

    [ContextMenu("Debug: CAM Renderers (لیست رندرهای زیر دوربین رویداد)")]
    private void DebugLogCctvRenderers()
    {
        CCTV_Camera cam = GetScareCctvCamera();
        if (cam == null) { Debug.Log("[Night1Scare] 🔎 دوربین رویداد پیدا نشد (CCTV خالیه؟)"); return; }

        Debug.Log($"[Night1Scare] 🔎 ===== رندرهای زیر دوربین رویداد (CAM {scareCameraNumber}) =====");

        List<Renderer> found = new List<Renderer>();
        if (cam.VirtualCamera != null) found.AddRange(cam.VirtualCamera.GetComponentsInChildren<Renderer>(true));
        if (cam.LiveCamera != null) found.AddRange(cam.LiveCamera.GetComponentsInChildren<Renderer>(true));

        if (found.Count == 0)
            Debug.LogWarning("[Night1Scare] 🔎 هیچ رندری زیر VirtualCamera/LiveCamera پیدا نشد!");

        for (int i = 0; i < found.Count; i++)
        {
            Renderer r = found[i];
            if (r == null) continue;

            bool isProtectedFx = IsProtectedCctvEffect(r.transform);
            bool glow = IsGlowLikeRenderer(r);
            string verdict = isProtectedFx ? "🛡️ محافظت‌شده (افکت دید در شب)"
                           : glow ? "🟢 قابل پنهان شدن (هاله/گلو)"
                           : "⚙️ معمولی";

            Debug.Log($"[Night1Scare] 🔎 رندر[{i}]: «{GetObjectPath(r.transform)}» | نوع: {r.GetType().Name} | " +
                      $"شیدر: {ShaderNameOf(r)} | enabled={r.enabled} | GO فعال={r.gameObject.activeSelf} → {verdict}");
        }

        Debug.Log($"[Night1Scare] 🔎 Force Hide ({(cctvGlareForceHide != null ? cctvGlareForceHide.Length : 0)} تا): {ListNames(cctvGlareForceHide)}");
        Debug.Log($"[Night1Scare] 🔎 Never Hide ({(cctvGlareNeverHide != null ? cctvGlareNeverHide.Length : 0)} تا): {ListNames(cctvGlareNeverHide)}");
        Debug.Log("[Night1Scare] 🔎 ===== پایان لیست رندرها =====");
    }

    private static string ListNames(Renderer[] list)
    {
        if (list == null || list.Length == 0) return "—";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < list.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(list[i] != null ? list[i].name : "null");
        }
        return sb.ToString();
    }

    private Transform GetScareCameraTransform()
    {
        int idx = scareCameraNumber - 1;
        if (cctv != null && cctv.Cameras != null && idx >= 0 && idx < cctv.Cameras.Length && cctv.Cameras[idx] != null)
        {
            Camera live = cctv.Cameras[idx].LiveCamera;
            if (live != null) return live.transform;
        }
        return null;
    }

    private bool IsEventBlocking()
    {
        if (this == null) return false;   // اگه آبجکت نابود شده بود
        return phase == Phase.Walking || phase == Phase.WaitingLights || phase == Phase.Vanishing;
    }

    // ------------------ ریست (اول هر شب) ------------------

    private void HandleNightStarted() => ResetEvent();

    /// <summary>ریست کامل رویداد — اول هر شب خودکار صدا زده میشه.</summary>
    public void ResetEvent()
    {
        phase = Phase.Idle;
        usedThisNight = false;
        unlocked = false;
        phaseTime = 0f;
        lightsTimer = 0f;
        stareTime = 0f;
        walkDone = false;
        timerWarned = false;
        stareWarned = false;
        headBone = null;
        stareTargetCam = null;

        // v1.3.9 — هاله‌های پنهان‌شده برگردن
        RestoreHiddenGlare();

        // جنراتور: اگه وسط رویداد خاموش شده بود، برای شب جدید دوباره روشنش کن
        if (genStoppedDuringEvent && restoreGeneratorOnReset && generator != null)
        {
            generator.StartGenerator();
            Debug.Log("[Night1Scare] جنراتور برای شب جدید دوباره روشن شد.");
        }
        genStoppedDuringEvent = false;
        genKilledByEvent = false;
        blackoutTimer = 0f;

        if (cctv != null)
            cctv.SetControlLocked(false);   // اطمینان

        if (scareMan != null)
        {
            scareMan.transform.SetPositionAndRotation(manStartPos, manStartRot);
            if (hideManWhenIdle)
                scareMan.SetActive(false);
        }

        if (restoreLightsOnReset)
            RestoreLights();
    }

    private void RestoreLights()
    {
        if (targetLights == null || initialLightEnabled == null) return;

        for (int i = 0; i < targetLights.Length && i < initialLightEnabled.Length; i++)
        {
            Light l = targetLights[i];
            if (l == null) continue;

            l.enabled = initialLightEnabled[i];
            l.gameObject.SetActive(initialLightGOActive[i]);
        }
    }

    // ------------------ v1.3.9: بیضی/هاله سبز روی CAM 5 ------------------
    // وقتی دوربین برای تعقیب هیولا می‌چرخه، بعضی هاله‌های نورانی صحنه (گلو چراغ‌ها، فلر لنز،
    // پارتیکل‌ها) وارد کادر میشن و چون دوربین ثابت بهشون خیره‌ست، نصف صفحه رو میگیرن.
    // راه‌حل: وسط رویداد این هاله‌ها موقتاً پنهان میشن و بعد از رویداد خودکار برمی‌گردن.

    private IEnumerator GlareScanRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.4f);

        while (phase == Phase.Walking || phase == Phase.WaitingLights || phase == Phase.Vanishing)
        {
            if (!autoHideCctvGlare)
            {
                RestoreHiddenGlare();
                yield break;
            }

            // وقتی از نمای دوربین خارج شدی، دنیای عادی باید دست‌نخورده باشه
            if (cctv != null && !cctv.IsCameraView)
                RestoreHiddenGlare();
            else
                HideGlareInCctvView();

            yield return wait;
        }

        RestoreHiddenGlare();
    }

    private void HideGlareInCctvView()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // v1.3.13 — اگه تجهیزات خود دوربین CAM قبلاً اشتباهی پنهان شده بودن، همین حالا برگردن
        RescueCctvOwnedGlare();

        // ۱) هاله‌های رندری (گلو کواد، اسپرایت، پارتیکل) که بزرگ روی صفحه‌ان
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer r = allRenderers[i];
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (hiddenGlareRenderers.Contains(r)) continue;
            if (IsInRendererList(r, cctvGlareNeverHide)) continue;                                 // v1.3.15 — لیست دستی «هیچ‌وقت»
            if (scareMan != null && r.transform.IsChildOf(scareMan.transform)) continue;          // خود مرد نه!
            if (r.GetComponentInParent<Canvas>() != null) continue;                                // UI نه
            if (cachedPlayerCam != null && r.transform.IsChildOf(cachedPlayerCam.transform.root)) continue; // پلیر نه

            // v1.3.15 — تجهیزات خود دوربین CAM: فقط «افکت دید در شب» محافظت میشه؛
            // هاله‌ی گلوِ زیر Spot Light اجازه داره پنهان شه (همون بیضی سبز!)
            if (IsOwnedByAnyCctvCamera(r.transform) && IsProtectedCctvEffect(r.transform))
            {
                LogProtectedCctvCandidateOnce(cam, r);
                continue;
            }

            bool forceHide = IsInRendererList(r, cctvGlareForceHide);   // v1.3.15 — لیست دستی «همیشه»

            if (!forceHide && !IsGlowLikeRenderer(r)) continue;

            float size = RendererScreenSizeFraction(cam, r, out bool inFront, out float dist);
            if (!inFront || size < glareMinScreenSize) continue;

            r.enabled = false;
            hiddenGlareRenderers.Add(r);
            DisableGlowScriptsAround(r, 3);
            glareRestoreLogged = false;

            if (forceHide)
                Debug.Log($"[Night1Scare] 🟢📌 هاله‌ی «Force Hide» پنهان شد: «{GetObjectPath(r.transform)}» ({size * 100f:0}٪ صفحه).");
            else if (IsOwnedByAnyCctvCamera(r.transform))
                Debug.Log($"[Night1Scare] 🟢🎯 هاله‌ی خود دوربین CAM پنهان شد: «{GetObjectPath(r.transform)}» " +
                          $"(شیدر: {ShaderNameOf(r)}, {size * 100f:0}٪ صفحه، فاصله {dist:0.#}م) — همون بیضی سبز بود؛ " +
                          "اگه بعد از این افکت دید در شب خراب شد، همین لاگ رو بفرست.");
            else
                Debug.Log($"[Night1Scare] 🟢 هاله‌ی بزرگ روی صفحه پنهان شد: «{GetObjectPath(r.transform)}» " +
                          $"(شیدر: {ShaderNameOf(r)}, {size * 100f:0}٪ صفحه، فاصله {dist:0.#}م) — احتمالاً همین بیضی سبز بود.");
        }

        // ۲) فلرهای چراغ‌ها (کلاسیک + SRP) که توی کادرن
        Light[] allLights = FindObjectsOfType<Light>();
        for (int i = 0; i < allLights.Length; i++)
        {
            Light l = allLights[i];
            if (l == null) continue;
            if (IsOwnedByAnyCctvCamera(l.transform)) continue;   // v1.3.13 — فلر/هاله‌ی خود دوربین CAM دست نمی‌زنیم

            Vector3 vp = cam.WorldToViewportPoint(l.transform.position);
            bool inView = vp.z > 0f && vp.x > -0.4f && vp.x < 1.4f && vp.y > -0.4f && vp.y < 1.4f;
            if (!inView) continue;

            if (l.flare != null && !hiddenFlareLights.Contains(l))
            {
                hiddenFlareLights.Add(l);
                hiddenFlareValues.Add(l.flare);
                l.flare = null;
                glareRestoreLogged = false;
                Debug.Log($"[Night1Scare] 🟢 فلر چراغ «{l.name}» موقتاً خاموش شد (توی کادر CAM بود).");
            }

            Component[] comps = l.GetComponents<Component>();
            for (int c = 0; c < comps.Length; c++)
            {
                Component comp = comps[c];
                if (comp == null) continue;
                if (!comp.GetType().Name.Contains("LensFlare")) continue;

                Behaviour b = comp as Behaviour;
                if (b == null || !b.enabled || hiddenFlareComponents.Contains(b)) continue;

                b.enabled = false;
                hiddenFlareComponents.Add(b);
                glareRestoreLogged = false;
                Debug.Log($"[Night1Scare] 🟢 کامپوننت فلر ({comp.GetType().Name}) روی «{l.name}» موقتاً خاموش شد.");
            }
        }
    }

    // ------------------ v1.3.13 + v1.3.15: محافظت «هوشمند» از تجهیزات خود دوربین‌های CAM ------------------

    /// <summary>آیا این ترنسفورم بخشی از تجهیزات خودِ دوربین‌های CCTV هست؟ (افکت دید در شب، اسپات‌لایت، هاله‌شون)</summary>
    private bool IsOwnedByAnyCctvCamera(Transform t)
    {
        if (t == null || cctv == null || cctv.Cameras == null) return false;
        for (int i = 0; i < cctv.Cameras.Length; i++)
        {
            CCTV_Camera cam = cctv.Cameras[i];
            if (cam == null) continue;
            if (cam.VirtualCamera != null && t.IsChildOf(cam.VirtualCamera.transform)) return true;
            if (cam.LiveCamera != null && t.IsChildOf(cam.LiveCamera.transform)) return true;
        }
        return false;
    }

    /// <summary>اگه چیزِ متعلق به دوربین CAM توی لیست‌های پنهان بود، فوراً برش گردون</summary>
    private void RescueCctvOwnedGlare()
    {
        bool rescued = false;

        for (int i = hiddenGlareRenderers.Count - 1; i >= 0; i--)
        {
            Renderer r = hiddenGlareRenderers[i];
            // v1.3.15 — فقط افکت دید در شب «نجات» میشه؛ هاله‌ی گلوِ خود دوربین باید پنهان بمونه
            if (r != null && IsOwnedByAnyCctvCamera(r.transform) && IsProtectedCctvEffect(r.transform))
            {
                r.enabled = true;
                hiddenGlareRenderers.RemoveAt(i);
                rescued = true;
            }
        }

        for (int i = hiddenFlareLights.Count - 1; i >= 0; i--)
        {
            Light l = hiddenFlareLights[i];
            if (l != null && IsOwnedByAnyCctvCamera(l.transform))
            {
                l.flare = hiddenFlareValues[i];
                hiddenFlareLights.RemoveAt(i);
                hiddenFlareValues.RemoveAt(i);
                rescued = true;
            }
        }

        for (int i = hiddenFlareComponents.Count - 1; i >= 0; i--)
        {
            Behaviour b = hiddenFlareComponents[i];
            if (b != null && IsOwnedByAnyCctvCamera(b.transform))
            {
                b.enabled = true;
                hiddenFlareComponents.RemoveAt(i);
                rescued = true;
            }
        }

        for (int i = hiddenGlowScripts.Count - 1; i >= 0; i--)
        {
            MonoBehaviour mb = hiddenGlowScripts[i];
            // v1.3.15 — اسکریپت گلوِ خود دوربین هم باید خاموش بمونه (فقط افکت دید در شب نجات میشه)
            if (mb != null && IsOwnedByAnyCctvCamera(mb.transform) && IsProtectedCctvEffect(mb.transform))
            {
                mb.enabled = true;
                hiddenGlowScripts.RemoveAt(i);
                rescued = true;
            }
        }

        if (rescued)
            Debug.Log("[Night1Scare] 🟢✅ تجهیزات خود دوربین CAM (افکت دید در شب/هاله‌ی نور) که اشتباهی پنهان شده بودن، برگشتند!");
    }

    // ------------------ v1.3.15: محافظت فقط برای افکت دید در شب ------------------

    /// <summary>آیا این ترنسفورم بخشی از «افکت دید در شب» خود دوربینه و نباید پنهان شه؟
    /// (هر چیزی زیر Volume، یا اسمش شبیه اورلی/نویز/استاتیک/وینیت/اسکن‌لاین هست)
    /// توجه: هاله‌ی گلوِ زیر Spot Light این محافظت رو «نداره» — چون همون بیضی سبزه!</summary>
    private bool IsProtectedCctvEffect(Transform t)
    {
        if (t == null) return false;

        // هر چیزی زیر سلسله‌مراتب Volume (Camera Volume = افکت دید در شب) محافظت میشه
        if (t.GetComponentInParent<UnityEngine.Rendering.Volume>() != null) return true;

        string n = t.name.ToLower();
        return n.Contains("volume") || n.Contains("overlay") || n.Contains("nightvision") ||
               n.Contains("night vision") || n.Contains("noise") || n.Contains("grain") ||
               n.Contains("static") || n.Contains("vignette") || n.Contains("scanline") ||
               n.Contains("crt") || n.Contains("postfx") || n.Contains("post fx");
    }

    private bool IsInRendererList(Renderer r, Renderer[] list)
    {
        if (r == null || list == null) return false;
        for (int i = 0; i < list.Length; i++)
            if (list[i] == r) return true;
        return false;
    }

    private static string ShaderNameOf(Renderer r)
    {
        return (r.sharedMaterial != null && r.sharedMaterial.shader != null) ? r.sharedMaterial.shader.name : "؟";
    }

    /// <summary>v1.3.15 — رندرهای بزرگِ محافظت‌شده‌ی CAM یک بار در هر رویداد لاگ میشن؛
    /// اگه بیضی سبز هنوز موند، این لاگ دقیق میگه کدوم آبجکته.</summary>
    private void LogProtectedCctvCandidateOnce(Camera cam, Renderer r)
    {
        if (r == null || protectedCandidateLogged.Contains(r)) return;

        float size = RendererScreenSizeFraction(cam, r, out bool inFront, out float dist);
        if (!inFront || size < glareMinScreenSize) return;

        protectedCandidateLogged.Add(r);
        Debug.Log($"[Night1Scare] 🛡️ رندرِ بزرگِ متعلق به دوربین CAM محافظت شد (پنهان نمیشه): «{GetObjectPath(r.transform)}» " +
                  $"| شیدر: {ShaderNameOf(r)} | {size * 100f:0}٪ صفحه، فاصله {dist:0.#}م — اگه همین بیضی سبز روی صفحه‌ست، " +
                  "همین لاگ رو بفرست یا آبجکتش رو توی فیلد «Cctv Glare Force Hide» بنداز تا پنهان بشه.");
    }

    private bool IsGlowLikeRenderer(Renderer r)
    {
        if (r is ParticleSystemRenderer || r is SpriteRenderer) return true;

        if (r.sharedMaterial != null && r.sharedMaterial.shader != null)
        {
            string sh = r.sharedMaterial.shader.name.ToLower();
            if (sh.Contains("additive") || sh.Contains("particles") || sh.Contains("glow") ||
                sh.Contains("flare") || sh.Contains("sprite") || sh.Contains("self-illumin") ||
                sh.Contains("unlit/transparent") || sh.Contains("unlit - transparent"))
                return true;
        }

        // اسکریپت‌های گلو (مثل LightSourceGlow خود کیت) روی خود آبجکت یا حداکثر ۳ سطح بالاتر
        return FindGlowScriptAround(r, 3) != null;
    }

    private MonoBehaviour FindGlowScriptAround(Renderer r, int maxUpLevels)
    {
        Transform t = r.transform;
        for (int level = 0; t != null && level <= maxUpLevels; level++)
        {
            MonoBehaviour[] mbs = t.GetComponents<MonoBehaviour>();
            for (int i = 0; i < mbs.Length; i++)
            {
                if (mbs[i] == null) continue;
                string n = mbs[i].GetType().Name.ToLower();
                if (n.Contains("glow") || n.Contains("lightsource"))
                    return mbs[i];
            }
            t = t.parent;
        }
        return null;
    }

    private void DisableGlowScriptsAround(Renderer r, int maxUpLevels)
    {
        Transform t = r.transform;
        for (int level = 0; t != null && level <= maxUpLevels; level++)
        {
            MonoBehaviour[] mbs = t.GetComponents<MonoBehaviour>();
            for (int i = 0; i < mbs.Length; i++)
            {
                MonoBehaviour mb = mbs[i];
                if (mb == null || !mb.enabled || hiddenGlowScripts.Contains(mb)) continue;

                string n = mb.GetType().Name.ToLower();
                if (n.Contains("glow") || n.Contains("lightsource"))
                {
                    mb.enabled = false;
                    hiddenGlowScripts.Add(mb);
                }
            }
            t = t.parent;
        }
    }

    private float RendererScreenSizeFraction(Camera cam, Renderer r, out bool inFront, out float distance)
    {
        inFront = false;
        distance = 0f;

        Bounds b = r.bounds;
        distance = Vector3.Distance(cam.transform.position, b.center);

        Vector3 c = b.center;
        Vector3 e = b.extents;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        int frontCount = 0;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = c + Vector3.Scale(e, new Vector3(x, y, z));
                    Vector3 vp = cam.WorldToViewportPoint(corner);
                    if (vp.z > -0.5f)
                    {
                        frontCount++;
                        if (vp.x < minX) minX = vp.x;
                        if (vp.x > maxX) maxX = vp.x;
                        if (vp.y < minY) minY = vp.y;
                        if (vp.y > maxY) maxY = vp.y;
                    }
                }
            }
        }

        inFront = frontCount > 0;
        if (!inFront) return 0f;

        return Mathf.Max(maxY - minY, maxX - minX);
    }

    private void RestoreHiddenGlare()
    {
        if (hiddenGlareRenderers.Count == 0 && hiddenFlareLights.Count == 0 &&
            hiddenFlareComponents.Count == 0 && hiddenGlowScripts.Count == 0)
            return;

        for (int i = 0; i < hiddenGlareRenderers.Count; i++)
            if (hiddenGlareRenderers[i] != null) hiddenGlareRenderers[i].enabled = true;
        hiddenGlareRenderers.Clear();

        for (int i = 0; i < hiddenFlareLights.Count; i++)
            if (hiddenFlareLights[i] != null) hiddenFlareLights[i].flare = hiddenFlareValues[i];
        hiddenFlareLights.Clear();
        hiddenFlareValues.Clear();

        for (int i = 0; i < hiddenFlareComponents.Count; i++)
            if (hiddenFlareComponents[i] != null) hiddenFlareComponents[i].enabled = true;
        hiddenFlareComponents.Clear();

        for (int i = 0; i < hiddenGlowScripts.Count; i++)
            if (hiddenGlowScripts[i] != null) hiddenGlowScripts[i].enabled = true;
        hiddenGlowScripts.Clear();

        if (!glareRestoreLogged)
        {
            glareRestoreLogged = true;
            Debug.Log("[Night1Scare] هاله‌های پنهان‌شده همه برگشتند (خارج از نمای CAM).");
        }
    }

    private static string GetObjectPath(Transform t)
    {
        string path = t.name;
        Transform p = t.parent;
        int depth = 0;
        while (p != null && depth++ < 4)
        {
            path = p.name + "/" + path;
            p = p.parent;
        }
        return path;
    }

    private class CulpritRow
    {
        public Renderer renderer;
        public float size;
        public float dist;
    }

    [ContextMenu("Debug: Log Green Ellipse Culprit (بیضی سبز چی‌ه؟)")]
    private void DebugLogEllipseCulprit()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[Night1Scare] Camera.main پیدا نشد — اول وارد نمای CAM 5 شو، وقتی بیضی سبز روی صفحه‌ست این منو رو بزن.");
            return;
        }

        Debug.Log($"[Night1Scare] 🔍 اسکن از دید «{cam.name}» ({cam.transform.position}) — بزرگ‌ترین چیزهای روی صفحه:");

        List<CulpritRow> rows = new List<CulpritRow>();
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer r = allRenderers[i];
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (r.GetComponentInParent<Canvas>() != null) continue;

            float size = RendererScreenSizeFraction(cam, r, out bool inFront, out float dist);
            if (!inFront || size < 0.08f) continue;

            rows.Add(new CulpritRow { renderer = r, size = size, dist = dist });
        }

        rows.Sort((a, b) => b.size.CompareTo(a.size));

        int logged = 0;
        for (int i = 0; i < rows.Count && logged < 15; i++)
        {
            logged++;
            Renderer r = rows[i].renderer;

            string shader = (r.sharedMaterial != null && r.sharedMaterial.shader != null)
                ? r.sharedMaterial.shader.name : "؟";

            bool emissive = false;
            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_EmissionColor"))
                emissive = r.sharedMaterial.GetColor("_EmissionColor").maxColorComponent > 0.05f;

            string extras = "";
            if (r is ParticleSystemRenderer) extras += " [پارتیکل]";
            if (r is SpriteRenderer) extras += " [اسپرایت/بیلبورد]";
            if (FindGlowScriptAround(r, 3) != null) extras += " [اسکریپت گلو]";
            if (scareMan != null && r.transform.IsChildOf(scareMan.transform)) extras += " [مرد ترسناک]";

            Debug.Log($"[Night1Scare]    {logged}) «{GetObjectPath(r.transform)}» — {rows[i].size * 100f:0}٪ صفحه | " +
                      $"فاصله {rows[i].dist:0.#}م | شیدر: {shader}" +
                      $"{(emissive ? " | نورانی (Emission → احتمال Bloom)" : "")}{extras}");
        }

        if (logged == 0)
            Debug.Log("[Night1Scare] هیچ مدل بزرگ‌تر از ۸٪ صفحه پیدا نشد — پس بیضی سبز مدل نیست؛ احتمالاً فلر چراغه (پایین رو ببین).");

        // فلرهای چراغ‌های توی کادر
        Light[] allLights = FindObjectsOfType<Light>();
        int flareCount = 0;
        for (int i = 0; i < allLights.Length; i++)
        {
            Light l = allLights[i];
            if (l == null) continue;

            bool classicFlare = l.flare != null;
            bool srpFlare = false;
            Component[] comps = l.GetComponents<Component>();
            for (int c = 0; c < comps.Length; c++)
            {
                if (comps[c] != null && comps[c].GetType().Name.Contains("LensFlare"))
                {
                    srpFlare = true;
                    break;
                }
            }
            if (!classicFlare && !srpFlare) continue;

            Vector3 vp = cam.WorldToViewportPoint(l.transform.position);
            if (vp.z > 0f && vp.x > -0.4f && vp.x < 1.4f && vp.y > -0.4f && vp.y < 1.4f)
            {
                flareCount++;
                Debug.Log($"[Night1Scare]    ☀️ چراغ «{l.name}» توی کادر دوربینه و {(classicFlare ? "فلر کلاسیک" : "فلر (SRP)")} داره — فلر دقیقاً شبیه یه بیضی نورانی بزرگ دیده میشه!");
            }
        }

        if (flareCount > 0)
            Debug.Log("[Night1Scare] ✅ تشخیص: به احتمال زیاد بیضی سبز = فلر/هاله‌ی چراغ‌های بالا. گزینه «Auto Hide Cctv Glare» خودش پنهانش میکنه.");
        else
            Debug.Log("[Night1Scare] فلری هم پیدا نشد. اگه هیچ‌کدوم از لیست بالا شبیه بیضی سبز نبود، یه اسکرین‌شات از صحنه بفرست تا دقیق بگم کدوم آبجکته.");
    }

    // ------------------ دیباگ ------------------

    [ContextMenu("Debug: Start Scare Now")]
    private void DebugStartNow()
    {
        if (cctv == null) cctv = FindObjectOfType<CCTV_CameraSystem>();
        if (phase != Phase.Idle)
        {
            Debug.LogWarning("[Night1Scare] رویداد الان در جریانه.");
            return;
        }
        if (scareMan == null) { Debug.LogWarning("[Night1Scare] مدل مرد تعیین نشده!"); return; }
        StartEvent();
    }

    [ContextMenu("Debug: Jumpscare Now")]
    private void DebugJumpscareNow() => DoJumpscare();

    [ContextMenu("Debug: Stop Generator (شبیه‌سازی نجات)")]
    private void DebugStopGenerator()
    {
        if (generator == null) generator = FindObjectOfType<PowerGenerator>();
        if (generator != null)
            generator.StopGenerator();
        else
            Debug.LogWarning("[Night1Scare] جنراتوری پیدا نشد!");
    }

    [ContextMenu("Debug: Reset Event")]
    private void DebugReset() => ResetEvent();

    [ContextMenu("Debug: Print Animator Info (عیب‌یابی انیمیشن)")]
    private void DebugPrintAnimatorInfo()
    {
        Animator anim = manAnimator;
        if (anim == null && scareMan != null)
            anim = scareMan.GetComponentInChildren<Animator>(true);

        if (anim == null)
        {
            Debug.LogWarning("[Night1Scare] هیچ Animator ای روی مرد پیدا نشد! (مدل اصلاً Rig انسانی داره؟)");
            return;
        }

        Debug.Log($"[Night1Scare] Animator روی «{anim.gameObject.name}» | Enabled: {anim.enabled}");
        Debug.Log(anim.runtimeAnimatorController != null
            ? $"[Night1Scare] Controller: ✅ {anim.runtimeAnimatorController.name}"
            : "[Night1Scare] Controller: ❌ خالیه! کنترلر ScaryManAC رو توی فیلد Controller کامپوننت Animator بنداز");
        Debug.Log(anim.avatar != null
            ? $"[Night1Scare] Avatar: {anim.avatar.name} {(anim.isHuman ? "(Humanoid ✅)" : "(Humanoid نیست ⚠️ — تب Rig مدل رو چک کن)")}"
            : "[Night1Scare] Avatar: ❌ خالیه! (تب Rig مدل → Animation Type = Humanoid → Apply)");

        AnimatorControllerParameter[] pars = anim.parameters;
        if (pars == null || pars.Length == 0)
        {
            Debug.LogWarning("[Night1Scare] Controller هیچ Parameter ای نداره — دو تا Trigger به اسم Walk و LookCamera بساز!");
            return;
        }

        Debug.Log($"[Night1Scare] Parameter های Controller ({pars.Length} تا):");
        foreach (AnimatorControllerParameter p in pars)
            Debug.Log($"[Night1Scare]    • {p.name} → {p.type}");
        Debug.Log("[Night1Scare] ☝️ اسم Trigger ها باید دقیقاً با فیلدهای Walk Trigger / Look Trigger اسکریپت یکی باشه (حروف بزرگ/کوچک مهمه).");
    }

    [ContextMenu("Debug: Test Walk Trigger")]
    private void DebugTestWalkTrigger()
    {
        if (manAnimator == null && scareMan != null)
            manAnimator = scareMan.GetComponentInChildren<Animator>(true);

        if (manAnimator == null || string.IsNullOrEmpty(walkTrigger))
        {
            Debug.LogWarning("[Night1Scare] برای تست: فیلد Man Animator و Walk Trigger باید پر باشن!");
            return;
        }

        manAnimator.SetTrigger(walkTrigger);
        Debug.Log($"[Night1Scare] SetTrigger(\"{walkTrigger}\") دستی فرستاده شد — " +
                  "اگه انیمیشن اجرا شد = اسکریپت سالمه و مشکل از فیلدها بود. اگه اجرا نشد = مشکل از Controller/ترنزیشنه.");
    }

    private void OnDrawGizmosSelected()
    {
        if (scareMan == null) return;
        Gizmos.color = Color.red;
        if (scareTargetPoint != null)
        {
            Gizmos.DrawLine(scareMan.transform.position, scareTargetPoint.position);
            Gizmos.DrawWireSphere(scareTargetPoint.position, 0.3f);
        }
    }
}
