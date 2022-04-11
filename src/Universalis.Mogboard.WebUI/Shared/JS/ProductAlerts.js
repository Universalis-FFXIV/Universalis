import Popup from "./Popup";
import ButtonLoading from "./ButtonLoading";
import Modals from "./Modals";

class ProductAlerts
{
    constructor()
    {
        this.maxTriggers = 15;
        this.deleteId = null;
        this.editId = null;
        this.alert = this.getAlertTemplate();

        this.uiForm          = $('.alert_form');
        this.uiTriggers      = $('.alert_entries');
        this.uiAlerts        = $('.alerts_table');
        this.uiDeleteConfirm = $('.btn_alert_delete_confirm');
    }

    getAlertTemplate()
    {
        return {
            alert_item_id: mog.path == 'market' ? itemId : null,
            alert_name: null,
            alert_nq: null,
            alert_hq: null,
            alert_dc: null,
            alert_notify_discord: null,
            alert_notify_email: null,
            alert_triggers: [],
            alert_type: null,
        };
    }

    watch()
    {
        if (mog.path != 'market') {
            return;
        }

        this.uiForm.on('click', '.alert_trigger_add', event => { this.addCustomTrigger(event) });
        this.uiForm.on('click', '.alert_trigger_remove', event => { this.removeCustomTrigger(event) });
        this.uiForm.on('click', '.btn_create_alert', event => { this.saveAlert(event) });
        this.uiForm.on('click', '.btn_new_alert', event => { this.newAlert(event) });
        this.uiAlerts.on('click', '.btn_alert_delete', event => { this.deleteAlert(event) });
        this.uiAlerts.on('click', '.btn_alert_edit', event => { this.editAlert(event) });

        this.loadAlerts();

        this.uiDeleteConfirm.on('click', event => { this.deleteAlertConfirmation() });

        Modals.add(
            $('.alert_info'),
            $('.alert_info_button button')
        );
    }

    /**
     * Add a new custom trigger to the alert form ui
     */
    addCustomTrigger(event)
    {
        event.preventDefault();

        if (this.alert.alert_triggers.length >= this.maxTriggers) {
            Popup.error('Max Triggers Reached', `You can add a maximum of ${this.maxTriggers} to a single alert. Sorry!`);
            return;
        }

        const trigger = {
            id: Math.floor((Math.random() * 99999) + 1),
            alert_trigger_field: this.uiForm.find('#alert_trigger_field').val().trim(),
            alert_trigger_op:    this.uiForm.find('#alert_trigger_op').val().trim(),
            alert_trigger_value: this.uiForm.find('#alert_trigger_value').val().trim(),
        };

        this.addCustomTriggerVisual(trigger, true);
    }

    /**
     * Adds a custom trigger
     */
    addCustomTriggerVisual(trigger, isButtonPress)
    {
        // check a trigger exists
        if (trigger.alert_trigger_value.length === 0) {
            Popup.error('Invalid Condition Value', 'The triggers condition value is empty.');
            return;
        }

        // the type of triggers must match
        const alertType = trigger.alert_trigger_field.split('_')[0];

        if (isButtonPress) {
            if (this.alert.alert_type == null) {
                this.alert.alert_type = alertType;
            } else if (alertType != this.alert.alert_type) {
                Popup.error('Mismatch Data Types', 'You cannot mix Prices or History triggers in the same alert.');
                return;
            }

            // check bool type
            if (['Prices_IsCrafted', 'Prices_IsHQ', 'Prices_HasMateria', 'History_IsHQ'].indexOf(trigger.alert_trigger_field) > -1) {
                if (['0', '1'].indexOf(trigger.alert_trigger_value) === -1) {
                    Popup.error('Invalid Condition Value', 'For the selected trigger field, <br> your trigger value must either be: <br> a 0 (False/No) OR a 1 (True/Yes).');
                    return;
                }

                if (['5', '6'].indexOf(trigger.alert_trigger_op) === -1) {
                    Popup.error('Invalid Operator', 'For the selected trigger field, <br> your trigger operator must be either: <br> = Equal-to OR != Not equal-to')
                    return;
                }
            }
        }

        // store
        this.alert.alert_triggers.push(trigger);

        // print trigger visual
        this.uiTriggers.append(`
            <div id="custom_trigger_${trigger.id}">
                <div><button type="button" class="alert_trigger_remove small" data-id="${trigger.id}"><i class="xiv-NavigationClose"></i></button></div>
                <div>
                    <code>
                        <span>${trigger.alert_trigger_field}</span>
                        <em>${alert_trigger_operators[trigger.alert_trigger_op]}:</em>
                        <strong>${trigger.alert_trigger_value}</strong>
                    </code>
                </div>
            </div>
        `);
    }

    /**
     * Remove custom triggers
     * @param event
     */
    removeCustomTrigger(event)
    {
        const id = $(event.currentTarget).attr('data-id');

        // remove row
        this.uiForm.find(`#custom_trigger_${id}`).remove();

        // find the id triggers and remove them
        this.alert.alert_triggers.forEach((trigger, index) => {
            if (trigger.id == id) {
                this.alert.alert_triggers.splice(index, 1);
            }
        });

        // reset alert type if no triggers
        if (this.alert.alert_triggers.length === 0) {
            this.alert.alert_type = null;
        }
    }

    /**
     * Create a new alert!
     * @param event
     */
    saveAlert(event)
    {
        event.preventDefault();

        this.alert.alert_name = this.uiForm.find('#alert_name').val().trim();
        this.alert.alert_nq = this.uiForm.find('#alert_nq').prop('checked');
        this.alert.alert_hq = this.uiForm.find('#alert_hq').prop('checked');
        this.alert.alert_dc = this.uiForm.find('#alert_dc').prop('checked');
        this.alert.alert_notify_discord = this.uiForm.find('#alert_notify_discord').prop('checked');
        this.alert.alert_notify_email = this.uiForm.find('#alert_notify_email').prop('checked');
        this.alert.alert_dps_perk = this.uiForm.find('#alert_dps_perk').prop('checked');

        if (this.alert.alert_name.length < 3 || this.alert.alert_name.length > 100) {
            Popup.error('Name Length', 'Please keep your alert name <br> between 4 and 100 characters');
            return;
        }

        if (this.alert.alert_hq === false && this.alert.alert_nq === false) {
            Popup.error('Error: HQ/NQ', 'You must choose either: <br> Normal Quality, High-Quality or Both.');
            return;
        }

        if (this.alert.alert_notify_discord === false && this.alert.alert_notify_email === false) {
            Popup.error('Error: Notification', 'You must choose to be notified <br> either by discord or by email.');
            return;
        }

        if (this.alert.alert_triggers.length === 0) {
            Popup.error('No Triggers', 'You have not set any triggers <br> on your alert, fix that!');
            return;
        }

        const submitButton = this.uiForm.find('.btn_create_alert');
        ButtonLoading.start(submitButton);

        // build edit url
        const url = this.editId ? mog.urls.alerts.update.replace('-id-', this.editId) : mog.urls.alerts.create;

        $.ajax({
            url: url,
            type: 'POST',
            dataType: "json",
            data: JSON.stringify(this.alert),
            contentType: "application/json",
            success: response => {
                this.loadAlerts();

                let state   = response[0];
                let message = response[1];

                if (message.length < 1) {
                    message = "(No response message)"
                }

                if (state) {
                    Popup.success('Alert Saved', message);
                    this.loadAlerts();
                    return;
                }

                Popup.success('Alert not created', `Could not create alert, reason: ${message}`);
            },
            error: (a,b,c) => {
                Popup.error('Error 23', 'Could not create the alert. Ask Vek why!');
                console.log('--- ERROR ---');
                console.log(a,b,c)
            },
            complete: () => {
                ButtonLoading.finish(submitButton);
            }
        })
    }

    /**
     * Load alerts
     */
    loadAlerts()
    {
        if (mog.isOnline === false) {
            return;
        }

        $.ajax({
            url: mog.urls.alerts.renderForItem.replace('-id-', itemId),
            success: response => {
                this.uiAlerts.html(response);

                Modals.add(
                    $('.alert_delete_confirmation_modal'),
                    $('.btn_alert_delete')
                );
            },
            error: (a,b,c) => {
                this.uiAlerts.html('<div class="alert-red">Could not load any alerts for this item.</div>');
                console.log('--- ERROR ---');
                console.log(a,b,c)
            }
        })
    }

    /**
     * Edit an alert
     */
    editAlert(event)
    {
        // reset form
        this.newAlert();

        const button = $(event.currentTarget);
        this.editId  = button.attr('data-id');

        ButtonLoading.start(button);
        this.toggleAlertFormLoading(true);

        $.ajax({
            url: mog.urls.alerts.edit.replace('-id-', this.editId),
            type: 'GET',
            success: response => {
                this.editId = response.id;
                this.uiForm.find('#alert_name').val(response.alert_name);
                this.uiForm.find('#alert_nq').prop('checked', response.alert_nq);
                this.uiForm.find('#alert_hq').prop('checked', response.alert_hq);
                this.uiForm.find('#alert_dc').prop('checked', response.alert_dc);
                this.uiForm.find('#alert_notify_discord').prop('checked', response.alert_notify_discord);
                this.uiForm.find('#alert_notify_email').prop('checked', response.alert_notify_email);
                this.uiForm.find('#alert_dps_perk').prop('checked', response.alert_dps_perk);

                this.alert.alert_name = response.alert_name;
                this.alert.alert_nq = response.alert_nq;
                this.alert.alert_hq = response.alert_hq;
                this.alert.alert_dc = response.alert_dc;
                this.alert.alert_notify_discord = response.alert_notify_discord;
                this.alert.alert_notify_email = response.alert_notify_email;
                this.alert.alert_dps_perk = response.alert_dps_perk;

                // find the id triggers and remove them
                response.triggers.forEach((trigger, index) => {
                    this.addCustomTriggerVisual({
                        id: Math.floor((Math.random() * 99999) + 1),
                        alert_trigger_field:        trigger[0],
                        alert_trigger_op:           trigger[1],
                        alert_trigger_value:        trigger[2],
                    })
                });

                $('.alert_form').addClass('edit_mode');
            },
            error: (a,b,c) => {
                Popup.error('Error 8456', 'Could not fetch alert to edit.');
                console.log('--- ERROR ---');
                console.log(a,b,c);
            },
            complete: () => {
                ButtonLoading.finish(button);
                this.toggleAlertFormLoading(false);
            }
        });
    }

    /**
     * Delete alert
     */
    deleteAlert(event)
    {
        this.deleteId = $(event.currentTarget).attr('data-id');

        const alertName = $(event.currentTarget).parents('tr').attr('data-name');
        const confirmModal = $('.alert_delete_confirmation_modal');
        confirmModal.find('h1').html(alertName);
    }

    /**
     * Delete alert confirmation
     */
    deleteAlertConfirmation()
    {
        const url = mog.urls.alerts.delete.replace('-id-', this.deleteId);
        ButtonLoading.start(this.uiDeleteConfirm);

        // reset
        this.newAlert();

        $.ajax({
            url: url,
            type: 'GET',
            success: response => {
                this.loadAlerts();
                Popup.success('Alert Deleted', 'This alert has been deleted');
            },
            error: (a,b,c) => {
                Popup.error('Error 75', 'Could not delete alert.');
                console.log('--- ERROR ---');
                console.log(a,b,c)
            },
            complete: () => {
                ButtonLoading.finish(this.uiDeleteConfirm);
                Modals.close(
                    $('.alert_delete_confirmation_modal')
                );
            }
        });
    }

    /**
     * Reset the form
     */
    newAlert()
    {
        $('.alert_form').removeClass('edit_mode');

        this.deleteId = null;
        this.editId = null;
        this.alert = this.getAlertTemplate();

        this.uiForm.find('#alert_name').val('');
        this.uiForm.find('#alert_nq').prop('checked', false);
        this.uiForm.find('#alert_hq').prop('checked', false);
        this.uiForm.find('#alert_dc').prop('checked', false);
        this.uiForm.find('#alert_notify_discord').prop('checked', false);
        this.uiForm.find('#alert_notify_email').prop('checked', false);
        this.uiForm.find('#alert_dps_perk').prop('checked', false);

        this.uiTriggers.html('');

        this.uiForm.find('#alert_trigger_field option:first').prop('selected',true);
        this.uiForm.find('#alert_trigger_op option:first').prop('selected',true);
        this.uiForm.find('#alert_trigger_value').val('');
    }

    /**
     * Toggle loading state for alerts
     */
    toggleAlertFormLoading(on)
    {
        const $alertFormLoading = $('.alert-form-loading');
        on ? $alertFormLoading.addClass('show') : $alertFormLoading.removeClass('show');
    }
}

export default new ProductAlerts;
