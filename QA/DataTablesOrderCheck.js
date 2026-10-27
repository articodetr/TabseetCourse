// فحص سلوك DataTables 1.10.20 (نسخة المشروع) لقاعدة «الأحدث أولاً» - 2026-10-27
// يثبت أن الإعداد الافتراضي (بدون "order") يعيد فرز الجدول بالعمود الأول تصاعدياً فيقلب ترتيب الخادم،
// وأن "order": [] يحافظ على ترتيب الخادم عبر البحث والتنقل بين الصفحات مع بقاء الفرز اليدوي بالنقر على العنوان.
// التشغيل (مرة واحدة):  npm install jsdom@24   ثم:  node QA/DataTablesOrderCheck.js
const path = require('path');
const fs = require('fs');
const { JSDOM } = require('jsdom');
const root = path.join(__dirname, '..', 'tabseetCourse');
const jq = fs.readFileSync(path.join(root, 'Content/plugins/jquery/jquery.min.js'), 'utf8');
const dt = fs.readFileSync(path.join(root, 'Content/plugins/datatables/jquery.dataTables.min.js'), 'utf8');

function run(opts, label) {
  // ترتيب الخادم: 30 (الأحدث) ... 19 (الأقدم)
  const rows = [30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19].map(id => `<tr><td>${id}</td><td>Lec ${id}</td></tr>`).join('');
  const dom = new JSDOM(`<!doctype html><html><body><table id="example1"><thead><tr><th>ID</th><th>Name</th></tr></thead><tbody>${rows}</tbody></table></body></html>`, { runScripts: 'outside-only', pretendToBeVisual: true });
  const w = dom.window;
  w.eval(jq); w.eval(dt);
  const $ = w.jQuery;
  const t = $('#example1').DataTable(Object.assign({ paging: true, pageLength: 5, searching: true, ordering: true, info: true }, opts));
  const shown = () => $('#example1 tbody tr').map((i, tr) => $(tr).find('td').first().text()).get().join(',');
  const out = { label, initial: shown() };
  t.page(1).draw('page'); out.page2 = shown();
  t.search('Lec 2').draw(); out.search = shown();
  t.search('').draw(); t.page(0).draw('page');
  $('#example1 thead th').eq(0).trigger('click'); out.afterHeaderClick = shown();
  return out;
}

const def = run({}, 'default (no order option)');
const keep = run({ order: [] }, 'order: []');
console.log(JSON.stringify(def));
console.log(JSON.stringify(keep));
const ok = def.initial === '19,20,21,22,23'            // الافتراضي يقلب الترتيب (الأقدم أولاً)
  && keep.initial === '30,29,28,27,26'                // order: [] يحافظ على الأحدث أولاً
  && keep.page2 === '25,24,23,22,21'                  // الصفحة الثانية تكمل التسلسل
  && keep.search === '29,28,27,26,25'                 // البحث يحافظ على الترتيب
  && keep.afterHeaderClick === '19,20,21,22,23';      // الفرز اليدوي ما زال ممكناً
console.log(ok ? 'PASS: "order": [] keeps the server order through paging and search; manual sort still works' : 'FAIL');
process.exit(ok ? 0 : 1);
