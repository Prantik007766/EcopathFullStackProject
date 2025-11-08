window.EcoPath = (function(){
  const API_BASE = 'http://localhost:5085/api/v1';
  const runSplash = (selector, nextUrl) => {
    const el = document.querySelector(selector);
    if(!el) return;
    setTimeout(()=>{
      el.classList.add('fade-out');
      setTimeout(()=>{ window.location.href = nextUrl; }, 650);
    }, 3000);
    loadFactors();
  };

  const bindLogin = (formSel) => {
    const form = document.querySelector(formSel);
    if(!form) return;
    form.addEventListener('submit', (e)=>{
      e.preventDefault();
      const data = Object.fromEntries(new FormData(form).entries());
      if(!data.mineId || !data.password){
        alert('Please enter your mine id and password.');
        return;
      }
      // Placeholder: In real app, authenticate. Here we just route to app home.
      localStorage.setItem('ecopath_user', JSON.stringify({ mineId: data.mineId }));
      window.location.href = 'home.html';
    });
  };

  const bindRegister = (formSel) => {
    const form = document.querySelector(formSel);
    if(!form) return;
    form.addEventListener('submit', (e)=>{
      e.preventDefault();
      const data = Object.fromEntries(new FormData(form).entries());
      if(!data.mineName || !data.mineId || !data.location || !data.area || !data.email || !data.phone || !data.password || !data.confirmPassword){
        alert('Please fill all fields.');
        return;
      }
      if(data.password !== data.confirmPassword){
        alert('Passwords do not match.');
        return;
      }
      const profile = {
        mineName: data.mineName,
        mineId: data.mineId,
        location: data.location,
        area: data.area,
        email: data.email,
        phone: data.phone
      };
      try{
        // persist to backend
        fetch(`${API_BASE}/profile`, { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify(profile) })
          .then(()=>{ try{ localStorage.setItem('ecopath_user', JSON.stringify(profile)); }catch(_){} })
          .finally(()=>{ window.location.href = 'profile.html'; });
      }catch(_){ window.location.href = 'profile.html'; }
    });
  };

  // Gauges and Dashboard
  const pct = (num, den) => den === 0 ? 0 : Math.max(0, Math.min(100, Math.round((num/den)*100)));

  const renderGauge = (el, valuePct, label, valueText) => {
    if(!el) return;
    el.style.setProperty('--val', valuePct);
    const labelEl = el.querySelector('.center .label');
    const valueEl = el.querySelector('.center .value');
    if(labelEl) labelEl.textContent = label || '';
    if(valueEl){
      const parts = String(valueText||'').trim().split(/\s+/);
      const unit = parts.length>1 ? parts.pop() : '';
      const num = parts.join(' ');
      valueEl.innerHTML = `<span class="num">${num}</span> <span class="unit">${unit}</span>`;
    }
  };

  const renderRing = (el, valuePct, text) => {
    if(!el) return;
    el.style.setProperty('--val', valuePct);
    const span = el.querySelector('span');
    if(span) span.textContent = text ?? valuePct;
  };

  const initDashboard = (config) => {
    const data = Object.assign({
      totalEmissions:{ value:126456, unit:'tCO2e', delta:'+5.2% from last year', max:200000 },
      transportation:{ value:25678, unit:'tCO2e', delta:'+3.4% from last year', max:60000 },
      energy:{ value:85234, unit:'MWh', delta:'-2.1% from last year', max:120000 },
      credits:{ value:1105 },
      offsets:[
        { label:'Alternative Fuels', value:-1234 },
        { label:'Afforestation', value:-456 },
        { label:'Carbon Credit', value:-789 },
      ],
      // 12-month series (Jan–Dec)
      bars:[60,90,140,100,170,160, 120,135,150, 110,95,130],
      barLabels:['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec']
    }, config||{});

    const qs = (s)=>document.querySelector(s);

    // Sidebar toggle
    const toggle = qs('#sidebarToggle');
    const sidebar = qs('#sidebar');
    if(toggle && sidebar){
      toggle.addEventListener('click', ()=>{
        sidebar.classList.toggle('collapsed');
      });
    }

    // Total Emissions (metric card)
    const emissionsVal = qs('#emissionsVal');
    const emissionsBar = qs('#emissionsBar');
    const emissionsNote = qs('#emissionsNote');
    const applyEmissions = (val)=>{
      if(typeof val !== 'number' || isNaN(val)) val = 0;
      if(emissionsVal) emissionsVal.textContent = Number(val||0).toLocaleString();
      if(emissionsBar) emissionsBar.style.width = `${pct(val, data.totalEmissions.max)}%`;
      if(emissionsNote) emissionsNote.textContent = 'vs last year';
    };
    // Prefer API latest report summary, fallback to cached localStorage
    fetch(`${API_BASE}/reports/latest/summary`).then(r=>r.ok?r.json():null).then(s=>{
      if(s && typeof s.totalEmissions==='number'){
        applyEmissions(s.totalEmissions);
      }else{
        const stored = parseFloat(localStorage.getItem('ecopath_footprint_total'));
        applyEmissions(isNaN(stored)?0:stored);
      }
    }).catch(_=>{
      const stored = parseFloat(localStorage.getItem('ecopath_footprint_total'));
      applyEmissions(isNaN(stored)?0:stored);
    });

    // Transportation
    const transPct = pct(data.transportation.value, data.transportation.max);
    renderGauge(qs('#gTransport'), transPct, 'Transportation', `${data.transportation.value.toLocaleString()} ${data.transportation.unit}`);
    const transDelta = qs('#transDelta');
    if(transDelta) transDelta.textContent = data.transportation.delta;

    // Energy
    const energyPct = pct(data.energy.value, data.energy.max);
    renderGauge(qs('#gEnergy'), energyPct, 'Energy Consumption', `${data.energy.value.toLocaleString()} ${data.energy.unit}`);
    const energyDelta = qs('#energyDelta');
    if(energyDelta) energyDelta.textContent = data.energy.delta;

    // Credits ring
    renderRing(qs('#creditsRing'), pct(data.credits.value, 2000), String(data.credits.value));

    // Offsets list
    const list = qs('#offsetList');
    if(list){
      list.innerHTML = '';
      data.offsets.forEach(o=>{
        const row = document.createElement('div');
        row.className = 'row';
        const l = document.createElement('div');
        l.textContent = o.label;
        const v = document.createElement('div');
        v.textContent = `${o.value.toLocaleString()} tons`;
        row.appendChild(l); row.appendChild(v);
        list.appendChild(row);
      });
    }

    // Bars
    const barsWrap = qs('#bars');
    const axisX = qs('#axisX');
    if(barsWrap){
      barsWrap.innerHTML = '';
      // always render 12 bars (pad or slice)
      const months = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];
      const series = (data.bars||[]).slice(0,12);
      while(series.length<12) series.push(0);
      const max = Math.max(...series, 1);
      const labels = (data.barLabels && data.barLabels.length===12) ? data.barLabels : months;
      series.forEach((v, i)=>{
        const col = document.createElement('div');
        col.className = 'barcol';
        const b = document.createElement('div');
        b.className = 'bar';
        b.style.height = `${Math.round((v/max)*180)+12}px`;
        const lab = document.createElement('div');
        lab.className = 'bar-label';
        lab.textContent = labels[i] || months[i];
        col.appendChild(b);
        col.appendChild(lab);
        barsWrap.appendChild(col);
      });
    }
    if(axisX){ axisX.textContent = ''; }
  };

  // ECalculator
  const initCalculator = () => {
    const $ = (s, r=document)=>r.querySelector(s);
    const panels = document.querySelectorAll('.panel');
    if(!panels.length) return;

    // Populate mine dropdown from API
    const populateMines = async () => {
      try{
        const sel = $('#mineType');
        if(!sel) return;
        const res = await fetch(`${API_BASE}/mines`);
        if(!res.ok) return;
        const data = await res.json();
        sel.innerHTML = '<option value="">Select Mine</option>';
        data.forEach(m => {
          const opt = document.createElement('option');
          opt.value = m.id;
          opt.textContent = m.name;
          sel.appendChild(opt);
        });
      }catch(_){ /* ignore */ }
    };

    // kg CO2e per unit (simplified placeholders)
    let EF = {
      petrol: 2.31,     // kg CO2e per liter
      diesel: 2.68,     // kg CO2e per liter
      natural_gas: 2.02,// kg CO2e per m3 (approx)
      electricity: 0.75 // kg CO2e per kWh (grid default, adjust as needed)
    };

    const loadFactors = async () => {
      try{
        const res = await fetch(`${API_BASE}/factors`);
        if(!res.ok) return;
        const data = await res.json();
        const map = {};
        data.forEach(f=>{ map[f.code] = f.factor; });
        if(map.grid!=null) EF.electricity = map.grid;
        if(map.petrol!=null) EF.petrol = map.petrol;
        if(map.diesel!=null) EF.diesel = map.diesel;
        if(map.natural_gas!=null) EF.natural_gas = map.natural_gas;
        panels.forEach(calc);
      }catch(_){ }
    };

    const calc = (panel) => {
      const fuelSel = $('.fuel', panel);
      const qtyEl   = $('.qty', panel);
      const outEl   = $('.out', panel);
      if(!qtyEl || !outEl) return;

      const qty = parseFloat(qtyEl.value)||0;
      let kg = 0;
      if(panel.dataset.activity === 'electricity'){
        kg = qty * (EF.electricity||0);
      }else{
        const fuel = fuelSel ? fuelSel.value : '';
        const ef = EF[fuel] || 0;
        kg = qty * ef;
      }
      const tons = kg/1000; // convert kg to tCO2e
      outEl.textContent = tons.toFixed(2);
    };

    panels.forEach(panel=>{
      const fuelSel = $('.fuel', panel);
      const qtyEl   = $('.qty', panel);
      if(fuelSel) fuelSel.addEventListener('change', ()=>calc(panel));
      if(qtyEl)   qtyEl.addEventListener('input', ()=>calc(panel));
      // initial
      calc(panel);
    });

    // aggregate total
    const calcAll = async () => {
      try{
        const activities = Array.from(panels).map(p=>{
          const activity = p.dataset.activity||'';
          const qtyEl = $('.qty', p);
          const qty = parseFloat(qtyEl?.value||0) || 0;
          const fuelSel = $('.fuel', p);
          let fuel = fuelSel ? (fuelSel.value||'') : (activity==='electricity' ? 'grid' : '');
          if(activity==='electricity') fuel = 'grid';
          return { activity, fuelType: fuel, quantity: qty, unit: activity==='electricity'?'kWh':'' };
        });
        const res = await fetch(`${API_BASE}/calc/aggregate`, { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ activities }) });
        if(res.ok){
          const body = await res.json();
          const total = Number(body.totalTons||0);
          const dest = $('#totalFootprint');
          if(dest) dest.textContent = total.toFixed(2);
          try{ localStorage.setItem('ecopath_footprint_total', total.toFixed(2)); }catch(e){}
          // persist activities into current report
          try{
            const r = await fetch(`${API_BASE}/reports/current`, { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ title: 'Daily Report' }) });
            const rj = r.ok ? await r.json() : null;
            const rid = rj && (rj.id || rj.Id || rj.report?.id || rj.reportId || rj.report?.Id);
            if(rid){
              await fetch(`${API_BASE}/reports/${rid}/calc-entries/bulk`, { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ items: activities }) });
            }
          }catch(_){}
          return total;
        }
      }catch(_){ }
      let total = 0;
      document.querySelectorAll('.panel .out').forEach(el=>{ const v = parseFloat(el.textContent)||0; total += v; });
      const dest = $('#totalFootprint');
      if(dest) dest.textContent = total.toFixed(2);
      try{ localStorage.setItem('ecopath_footprint_total', total.toFixed(2)); }catch(e){}
      return total;
    };

    const btn = $('#calcTotal');
    if(btn){ btn.addEventListener('click', ()=>{ calcAll(); }); }

    // sidebar toggle reuse
    const sidebar = $('#sidebar');
    const toggle  = $('#sidebarToggle');
    if(sidebar && toggle){ toggle.addEventListener('click', ()=>sidebar.classList.toggle('collapsed')); }

    // initial data loads
    populateMines();
  };

  // Pathways (Reduction Strategies)
  const initPathways = () => {
    const $ = (s, r=document)=>r.querySelector(s);
    // guards
    const container = document.querySelector('.ecal-container');
    if(!container) return;

    // Assumptions (editable placeholders)
    const GRID_TON_PER_MWH = 0.75; // tCO2e per MWh (0.75 kg/kWh)
    const RENEW_CF = 0.35;         // capacity factor for RE
    const EV_DISPLACED_TONS_PER_YEAR = 4.0; // each EV avoids ~4 tCO2e/yr
    const GWP_CH4_CAPTURE = 27.0;  // CH4 global warming potential (capture/flare)
    const GWP_CH4_VAM = 20.0;      // conservative value for VAM oxidation

    const setText = (sel, v)=>{ const el=$(sel); if(el) el.textContent = Number(v).toFixed(2); };

    // EVs
    const evCalc = () => {
      const n = parseFloat($('#evCount')?.value||0);
      const perYear = (n||0) * EV_DISPLACED_TONS_PER_YEAR;
      setText('#evOut', perYear);
      return perYear;
    };

    // Renewable energy
    const reCalc = () => {
      const mw  = parseFloat($('#reMW')?.value||0);
      const pct = Math.max(0, Math.min(100, parseFloat($('#rePct')?.value||0)))/100;
      const mwhYear = mw * 8760 * RENEW_CF * pct;
      const tons = mwhYear * GRID_TON_PER_MWH;
      setText('#reOut', tons);
      return tons;
    };

    // Methane capture
    const mcCalc = () => {
      const ch4 = parseFloat($('#mcCH4')?.value||0); // tons CH4 captured
      const tons = ch4 * GWP_CH4_CAPTURE;            // tCO2e avoided
      setText('#mcOut', tons);
      return tons;
    };

    // VAM oxidation
    const vamCalc = () => {
      const ch4 = parseFloat($('#vamCH4')?.value||0); // tons CH4 processed
      const tons = ch4 * GWP_CH4_VAM;                 // tCO2e avoided
      setText('#vamOut', tons);
      return tons;
    };

    const calcAll = () => {
      const total = evCalc() + reCalc() + mcCalc() + vamCalc();
      setText('#reduceTotal', total);
      return total;
    };

    ['#evCount','#evYears','#reMW','#rePct','#mcCH4','#vamCH4'].forEach(sel=>{
      const el = $(sel); if(el){ el.addEventListener('input', calcAll); }
    });

    const btn = $('#calcReduce');
    if(btn){ btn.addEventListener('click', calcAll); }

    // sidebar toggle reuse
    const sidebar = $('#sidebar');
    const toggle  = $('#sidebarToggle');
    if(sidebar && toggle){ toggle.addEventListener('click', ()=>sidebar.classList.toggle('collapsed')); }

    // initial
    calcAll();
  };

  // Offset page (Afforestation + Carbon Credits)
  const initOffset = () => {
    const $ = (s, r=document)=>r.querySelector(s);
    const $$ = (s, r=document)=>Array.from(r.querySelectorAll(s));
    const panelAf = $('#afforestation');
    const panelCr = $('#credits');
    if(!panelAf && !panelCr) return;

    const TREE_CO2_TON_PER_YEAR = 0.022; // 22 kg CO2 per tree per year

    const baseline = parseFloat(localStorage.getItem('ecopath_footprint_total'))||0;
    const setText = (sel, v)=>{ const el=$(sel); if(el) el.textContent = Number(v).toFixed(2); };

    const calcAfforestation = () => {
      const area  = parseFloat($('#afArea')?.value||0); // hectares
      const tph   = parseFloat($('#afTrees')?.value||0); // trees per hectare
      const years = parseFloat($('#afYears')?.value||1); // years (optional scaling)
      const trees = (area||0) * (tph||0);
      const annual = trees * TREE_CO2_TON_PER_YEAR; // tons/year
      const remaining = Math.max(0, baseline - annual);
      setText('#afReduction', annual);
      setText('#afRemaining', remaining);
      // prefill carbon credit reduction if empty
      const crIn = $('#crReduction');
      if(crIn && !crIn.value){ crIn.value = annual.toFixed(2); }
      return annual;
    };

    const calcCredits = () => {
      const red  = parseFloat($('#crReduction')?.value||0); // tons reduced
      const rate = parseFloat($('#crRate')?.value||0);      // Rs per credit
      const credits = Math.max(0, red); // 1 credit per ton
      const value = credits * Math.max(0, rate);
      const tc = $('#crTotalCredits');
      const tco2 = $('#crTotalCO2');
      if(tc) tc.value = credits.toFixed(2);
      if(tco2) tco2.value = red.toFixed(2);
      const valField = $('#crValueField');
      if(valField) valField.value = value.toFixed(2);
      return {credits, value};
    };

    // bind
    ['#afArea','#afTrees','#afYears'].forEach(sel=>{ const el=$(sel); if(el) el.addEventListener('input', calcAfforestation); });
    const btn = $('#crCalc'); if(btn) btn.addEventListener('click', calcCredits);
    const redIn = $('#crReduction'); if(redIn) redIn.addEventListener('input', calcCredits);
    const rateIn = $('#crRate'); if(rateIn) rateIn.addEventListener('input', calcCredits);

    // sidebar toggle reuse
    const sidebar = $('#sidebar');
    const toggle  = $('#sidebarToggle');
    if(sidebar && toggle){ toggle.addEventListener('click', ()=>sidebar.classList.toggle('collapsed')); }

    // initial
    calcAfforestation();
    calcCredits();
  };

  return { runSplash, bindLogin, bindRegister, initDashboard, initCalculator, initPathways, initOffset };
})();
