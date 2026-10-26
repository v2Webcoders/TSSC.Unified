(function () {
  function initStudentEnrollmentChart() {
    var el = document.getElementById('studentEnrollmentChart');
    if (!el) return;
    new Chart(el.getContext('2d'), {
      type: 'line',
      data: {
        labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul'],
        datasets: [
          {
            label: 'This Year',
            data: [2000, 4200, 6800, 4500, 7200, 8600, 8200],
            borderColor: '#0f5fa8',
            backgroundColor: 'rgba(15,95,168,0.08)',
            borderWidth: 2,
            tension: 0.35,
            fill: true,
            pointRadius: 3,
            pointBackgroundColor: '#0f5fa8',
            pointBorderColor: '#fff'
          },
          {
            label: 'Last Year',
            data: [900, 1800, 3200, 2600, 4600, 6200, 5600],
            borderColor: '#c9ced9',
            backgroundColor: 'transparent',
            borderWidth: 2,
            borderDash: [4, 4],
            tension: 0.35,
            fill: false,
            pointRadius: 3,
            pointBackgroundColor: '#c9ced9',
            pointBorderColor: '#fff'
          }
        ]
      },
      options: {
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
          y: { grid: { color: '#eef2f7' }, ticks: { color: '#8091a7' } },
          x: { grid: { display: false }, ticks: { color: '#8091a7' } }
        }
      }
    });
  }

  function initTrainingStatsChart() {
    var el = document.getElementById('trainingStatsChart');
    if (!el) return;
    new Chart(el.getContext('2d'), {
      type: 'doughnut',
      data: {
        labels: ['Ongoing Trainings', 'Completed Trainings', 'Assessments', 'Certifications Issued'],
        datasets: [{
          data: [382, 612, 278, 456],
          backgroundColor: ['#0f5fa8', '#639922', '#f5821f', '#816bff'],
          borderWidth: 0
        }]
      },
      options: {
        cutout: '68%',
        maintainAspectRatio: false,
        plugins: { legend: { display: false } }
      }
    });
  }

  if (window.NioApp && NioApp.coms && NioApp.coms.docReady) {
    NioApp.coms.docReady.push(function () {
      initStudentEnrollmentChart();
      initTrainingStatsChart();
    });
  } else {
    document.addEventListener('DOMContentLoaded', function () {
      initStudentEnrollmentChart();
      initTrainingStatsChart();
    });
  }
})();
